// state
let state = null;
let stateLoadPromise = null;
let registrationQueue = Promise.resolve();

// deployment constants
const DEPLOYMENT_NAME = self.location.pathname.substring(0, self.location.pathname.lastIndexOf("/")).replaceAll("/", "-");
const DB_NAME = "xshell-sw" + DEPLOYMENT_NAME;
const DB_VERSION = 1;
const STORE_NAME = "state";

// resolve MIME type from a resource path
function getMimeType(path) {
    const index = path.lastIndexOf(".");
    const extension = index < 0 ? "" : path.substring(index).toLowerCase();
    return ({
        ".js": "text/javascript",
        ".mjs": "text/javascript",
        ".css": "text/css",
        ".json": "application/json",
        ".jsonc": "application/json",
        ".html": "text/html",
        ".htm": "text/html",
        ".svg": "image/svg+xml",
        ".png": "image/png",
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".webp": "image/webp",
        ".gif": "image/gif",
        ".ico": "image/x-icon",
        ".woff": "font/woff",
        ".woff2": "font/woff2",
        ".ttf": "font/ttf",
        ".otf": "font/otf",
        ".txt": "text/plain",
        ".md": "text/markdown",
        ".xml": "application/xml",
        ".wasm": "application/wasm"
    })[extension] || "application/octet-stream";
}

// handle ZIP-backed asset mappings
class ZipHandler {

    // ZIP format constants
    static EOCD_SIGNATURE = 0x06054b50;
    static CENTRAL_HEADER_SIGNATURE = 0x02014b50;
    static LOCAL_HEADER_SIGNATURE = 0x04034b50;
    static ZIP64_EXTRA_FIELD_ID = 0x0001;
    static MAX_COMMENT_LENGTH = 0xffff;
    static MAX_ENTRY_SIZE = 256 * 1024 * 1024;

    // ZIP handler state
    _cacheName = null;
    _promises = new Map();
    _crc32Table = null;

    // initialize ZIP cache and CRC table
    constructor(cacheName) {
        this._cacheName = cacheName;
        this._crc32Table = this._createCrc32Table();
    }

    // determine whether a destination is a ZIP package
    canHandle(url) {
        return new URL(url, self.location.origin).pathname.toLowerCase().endsWith(".zip");
    }

    // serve one virtual request from a ZIP package
    async handleRequest(request, rule, ruleSrcUrl, debug) {
        let entryName;

        // resolve and validate the requested package-relative entry path
        try {
            const requestUrl = new URL(request.url);
            const pathSuffix = requestUrl.pathname.substring(ruleSrcUrl.pathname.length);
            entryName = this._normalizeEntryPath(pathSuffix.startsWith("/") ? pathSuffix.substring(1) : pathSuffix);
        } catch (error) {
            if (debug) console.log("sw: invalid ZIP entry path: " + error.message);
            return new Response(null, { status: 400, statusText: "Bad Request" });
        }

        // load, extract, validate, and return the requested ZIP entry
        try {
            const zipUrl = new URL(rule.dst, self.location.origin);
            zipUrl.hash = "";

            const zip = await this._load(zipUrl.href);
            const entry = zip.entries.get(entryName);
            if (!entry) return new Response(null, { status: 404, statusText: "Not Found" });

            const body = await this._extractEntry(zip, entry);
            const headers = new Headers({
                "Content-Type": getMimeType(entryName),
                "Content-Length": String(body.byteLength)
            });

            return new Response(request.method === "HEAD" ? null : body, { status: 200, headers });
        } catch (error) {
            if (debug) console.log("sw: ZIP request failed: " + error.message);
            return new Response(null, { status: 502, statusText: "Bad Gateway" });
        }
    }

    // clear parsed ZIP packages from worker memory
    clearMemoryCache() {
        this._promises.clear();
    }

    // load a ZIP from memory, Cache Storage, or the network
    async _load(url) {
        if (this._promises.has(url)) return this._promises.get(url);

        const promise = (async () => {
            const cache = await caches.open(this._cacheName);
            let response = await cache.match(url);

            if (!response) {
                response = await fetch(url);
                if (!response.ok) throw new Error("ZIP package could not be loaded.");
                await cache.put(url, response.clone());
            } else if (!response.ok) {
                throw new Error("Cached ZIP package is not a successful response.");
            }

            return this._parse(new Uint8Array(await response.arrayBuffer()));
        })();

        this._promises.set(url, promise);

        // remove failed package loads so later requests can retry
        try {
            return await promise;
        } catch (error) {
            if (this._promises.get(url) === promise) this._promises.delete(url);
            throw error;
        }
    }

    // parse the ZIP central directory into an entry index
    _parse(bytes) {
        if (!(bytes instanceof Uint8Array)) throw new Error("ZIP package bytes are invalid.");

        const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
        const eocdOffset = this._findEocd(view, bytes.byteLength);

        // read end-of-central-directory metadata
        const diskNumber = view.getUint16(eocdOffset + 4, true);
        const centralDirectoryDisk = view.getUint16(eocdOffset + 6, true);
        const entriesOnDisk = view.getUint16(eocdOffset + 8, true);
        const entryCount = view.getUint16(eocdOffset + 10, true);
        const centralDirectorySize = view.getUint32(eocdOffset + 12, true);
        const centralDirectoryOffset = view.getUint32(eocdOffset + 16, true);

        // reject ZIP64 archives
        if (diskNumber === 0xffff || centralDirectoryDisk === 0xffff || entriesOnDisk === 0xffff || entryCount === 0xffff ||
            centralDirectorySize === 0xffffffff || centralDirectoryOffset === 0xffffffff) {
            throw new Error("ZIP64 archives are not supported.");
        }

        // reject multi-disk ZIP archives
        if (diskNumber !== 0 || centralDirectoryDisk !== 0 || entriesOnDisk !== entryCount) {
            throw new Error("Multi-disk ZIP archives are not supported.");
        }

        // validate the central directory range
        this._ensureRange(eocdOffset, centralDirectoryOffset, centralDirectorySize, "ZIP central directory is out of bounds.");

        const entries = new Map();
        const entryNames = new Set();
        const centralDirectoryEnd = centralDirectoryOffset + centralDirectorySize;
        let offset = centralDirectoryOffset;

        // parse every central directory entry
        for (let index = 0; index < entryCount; index++) {
            this._ensureRange(centralDirectoryEnd, offset, 46, "ZIP central directory header is truncated.");
            if (view.getUint32(offset, true) !== ZipHandler.CENTRAL_HEADER_SIGNATURE) {
                throw new Error("ZIP central directory signature is invalid.");
            }

            // read central directory entry metadata
            const flags = view.getUint16(offset + 8, true);
            const compressionMethod = view.getUint16(offset + 10, true);
            const crc = view.getUint32(offset + 16, true);
            const compressedSize = view.getUint32(offset + 20, true);
            const uncompressedSize = view.getUint32(offset + 24, true);
            const fileNameLength = view.getUint16(offset + 28, true);
            const extraFieldLength = view.getUint16(offset + 30, true);
            const commentLength = view.getUint16(offset + 32, true);
            const diskStart = view.getUint16(offset + 34, true);
            const localHeaderOffset = view.getUint32(offset + 42, true);

            // reject ZIP64 values embedded in entries
            if (compressedSize === 0xffffffff || uncompressedSize === 0xffffffff || localHeaderOffset === 0xffffffff || diskStart === 0xffff) {
                throw new Error("ZIP64 archives are not supported.");
            }

            // validate supported ZIP entry features
            if (diskStart !== 0) throw new Error("Multi-disk ZIP archives are not supported.");
            if ((flags & 0x0001) !== 0) throw new Error("Encrypted ZIP entries are not supported.");
            if (compressionMethod !== 0 && compressionMethod !== 8) throw new Error(`ZIP compression method ${compressionMethod} is not supported.`);
            if (uncompressedSize > ZipHandler.MAX_ENTRY_SIZE) throw new Error("ZIP entry exceeds the maximum extracted size.");

            // validate variable-length central directory fields
            const variableLength = fileNameLength + extraFieldLength + commentLength;
            this._ensureRange(centralDirectoryEnd, offset + 46, variableLength, "ZIP central directory entry is truncated.");

            const fileNameStart = offset + 46;
            const extraFieldStart = fileNameStart + fileNameLength;

            // reject ZIP64 extra fields
            if (this._hasExtraField(view, centralDirectoryEnd, extraFieldStart, extraFieldLength, ZipHandler.ZIP64_EXTRA_FIELD_ID)) {
                throw new Error("ZIP64 archives are not supported.");
            }

            // decode and validate the ZIP entry name
            const entryName = this._decodeFileName(bytes.subarray(fileNameStart, fileNameStart + fileNameLength), flags);
            this._validateArchiveEntryName(entryName);

            // reject duplicate ZIP entry names
            if (entryNames.has(entryName)) throw new Error(`ZIP archive contains duplicate entry '${entryName}'.`);
            entryNames.add(entryName);

            // add non-directory entries to the lookup index
            if (!entryName.endsWith("/")) {
                entries.set(entryName, {
                    entryName,
                    compressionMethod,
                    flags,
                    crc,
                    compressedSize,
                    uncompressedSize,
                    localHeaderOffset,
                    centralDirectoryOffset
                });
            }

            offset += 46 + variableLength;
        }

        // validate that the complete central directory was consumed
        if (offset !== centralDirectoryEnd) throw new Error("ZIP central directory size is invalid.");

        return { bytes, entries };
    }

    // locate the ZIP end-of-central-directory record
    _findEocd(view, byteLength) {
        if (byteLength < 22) throw new Error("ZIP end of central directory record is missing.");

        const minimumOffset = Math.max(0, byteLength - 22 - ZipHandler.MAX_COMMENT_LENGTH);

        // scan backwards because the EOCD is located at the end of the ZIP
        for (let offset = byteLength - 22; offset >= minimumOffset; offset--) {
            if (view.getUint32(offset, true) !== ZipHandler.EOCD_SIGNATURE) continue;

            const commentLength = view.getUint16(offset + 20, true);
            if (offset + 22 + commentLength === byteLength) return offset;
        }

        throw new Error("ZIP end of central directory record is missing.");
    }

    // inspect ZIP extra fields for a requested field identifier
    _hasExtraField(view, limit, offset, length, requestedId) {
        const end = offset + length;
        this._ensureRange(limit, offset, length, "ZIP extra field is out of bounds.");

        // parse each extra field in sequence
        while (offset < end) {
            this._ensureRange(end, offset, 4, "ZIP extra field header is truncated.");

            const id = view.getUint16(offset, true);
            const fieldLength = view.getUint16(offset + 2, true);
            offset += 4;

            this._ensureRange(end, offset, fieldLength, "ZIP extra field data is truncated.");
            if (id === requestedId) return true;

            offset += fieldLength;
        }

        return false;
    }

    // decode a ZIP entry filename as UTF-8
    _decodeFileName(bytes, flags) {
        try {
            return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
        } catch {
            throw new Error((flags & 0x0800) !== 0
                ? "ZIP entry has an invalid UTF-8 filename."
                : "ZIP entry filename is not UTF-8 compatible.");
        }
    }

    // normalize and validate a requested ZIP entry path
    _normalizeEntryPath(relativePath) {
        let decodedPath;

        // decode escaped URL path characters before validating traversal
        try {
            decodedPath = decodeURIComponent(relativePath);
        } catch {
            throw new Error("ZIP entry path is not valid percent-encoding.");
        }

        // reject absolute paths and Windows path separators
        if (!decodedPath || decodedPath.startsWith("/") || decodedPath.includes("\\")) {
            throw new Error("ZIP entry path is not package-root-relative.");
        }

        // reject empty, current-directory, and parent-directory segments
        const segments = decodedPath.split("/");
        if (segments.some(segment => !segment || segment === "." || segment === "..")) {
            throw new Error("ZIP entry path contains an invalid segment.");
        }

        return segments.join("/");
    }

    // validate an entry path stored inside the ZIP archive
    _validateArchiveEntryName(entryName) {
        if (!entryName || entryName.startsWith("/") || entryName.includes("\\")) {
            throw new Error("ZIP archive contains an unsafe entry name.");
        }

        // allow a final slash for directory entries but reject unsafe segments
        const path = entryName.endsWith("/") ? entryName.substring(0, entryName.length - 1) : entryName;
        const segments = path.split("/");
        if (segments.some(segment => !segment || segment === "." || segment === "..")) {
            throw new Error("ZIP archive contains an unsafe entry name.");
        }
    }

    // extract and validate one indexed ZIP entry
    async _extractEntry(zip, entry) {
        const view = new DataView(zip.bytes.buffer, zip.bytes.byteOffset, zip.bytes.byteLength);

        // locate and validate the local file header
        this._ensureRange(entry.centralDirectoryOffset, entry.localHeaderOffset, 30, "ZIP local file header is out of bounds.");
        if (view.getUint32(entry.localHeaderOffset, true) !== ZipHandler.LOCAL_HEADER_SIGNATURE) {
            throw new Error("ZIP local file header signature is invalid.");
        }

        // read local file header metadata
        const localFlags = view.getUint16(entry.localHeaderOffset + 6, true);
        const localCompressionMethod = view.getUint16(entry.localHeaderOffset + 8, true);
        const fileNameLength = view.getUint16(entry.localHeaderOffset + 26, true);
        const extraFieldLength = view.getUint16(entry.localHeaderOffset + 28, true);

        // validate local and central header consistency
        if ((localFlags & 0x0001) !== 0) throw new Error("Encrypted ZIP entries are not supported.");
        if (localFlags !== entry.flags || localCompressionMethod !== entry.compressionMethod) {
            throw new Error("ZIP local and central file headers do not match.");
        }

        // locate local filename and extra fields
        const fileNameStart = entry.localHeaderOffset + 30;
        this._ensureRange(entry.centralDirectoryOffset, fileNameStart, fileNameLength + extraFieldLength, "ZIP local file header is truncated.");

        // reject ZIP64 local extra fields
        if (this._hasExtraField(view, entry.centralDirectoryOffset, fileNameStart + fileNameLength, extraFieldLength, ZipHandler.ZIP64_EXTRA_FIELD_ID)) {
            throw new Error("ZIP64 archives are not supported.");
        }

        // ensure the local and central directory filenames match
        const localEntryName = this._decodeFileName(zip.bytes.subarray(fileNameStart, fileNameStart + fileNameLength), localFlags);
        if (localEntryName !== entry.entryName) throw new Error("ZIP local and central filenames do not match.");

        // locate and slice compressed entry bytes
        const dataStart = fileNameStart + fileNameLength + extraFieldLength;
        this._ensureRange(entry.centralDirectoryOffset, dataStart, entry.compressedSize, "ZIP entry data is out of bounds.");
        const compressedBytes = zip.bytes.subarray(dataStart, dataStart + entry.compressedSize);

        // decompress DEFLATE entries or copy stored entries
        const extractedBytes = entry.compressionMethod === 0
            ? compressedBytes.slice()
            : await this._inflateRaw(compressedBytes, entry.uncompressedSize);

        // validate the extracted byte length
        if (extractedBytes.byteLength !== entry.uncompressedSize) {
            throw new Error("ZIP entry uncompressed size does not match its central directory record.");
        }

        // validate the extracted entry checksum
        if (this._crc32(extractedBytes) !== entry.crc) throw new Error("ZIP entry CRC32 validation failed.");

        return extractedBytes;
    }

    // decompress raw DEFLATE bytes with the browser-native API
    async _inflateRaw(bytes, expectedSize) {
        const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("deflate-raw"));
        const reader = stream.getReader();
        const chunks = [];
        let length = 0;

        // read decompressed chunks while enforcing the declared size
        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                length += value.byteLength;
                if (length > expectedSize || length > ZipHandler.MAX_ENTRY_SIZE) {
                    throw new Error("ZIP entry expands beyond its declared size.");
                }

                chunks.push(value);
            }
        } catch (error) {
            await reader.cancel(error).catch(() => {});
            throw error;
        }

        // combine decompressed chunks into one byte array
        const result = new Uint8Array(length);
        let offset = 0;
        for (const chunk of chunks) {
            result.set(chunk, offset);
            offset += chunk.byteLength;
        }

        return result;
    }

    // build the lookup table used by CRC32 validation
    _createCrc32Table() {
        const table = new Uint32Array(256);

        for (let index = 0; index < table.length; index++) {
            let value = index;
            for (let bit = 0; bit < 8; bit++) {
                value = (value & 1) !== 0 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
            }
            table[index] = value >>> 0;
        }

        return table;
    }

    // calculate CRC32 for extracted entry bytes
    _crc32(bytes) {
        let value = 0xffffffff;
        for (const byte of bytes) value = this._crc32Table[(value ^ byte) & 0xff] ^ (value >>> 8);
        return (value ^ 0xffffffff) >>> 0;
    }

    // validate that a byte range stays inside its containing structure
    _ensureRange(limit, offset, length, message) {
        if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(length) || offset < 0 || length < 0 || offset > limit || length > limit - offset) {
            throw new Error(message);
        }
    }
}

// registered physical asset format handlers
const assetHandlers = [
    new ZipHandler("xshell-sw-packages" + DEPLOYMENT_NAME)
];

// open the persistent Service Worker state database
function openDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);

        req.onupgradeneeded = () => {
            const db = req.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) db.createObjectStore(STORE_NAME);
        };

        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

// persist one Service Worker state value
async function saveDBState(key, value) {
    const db = await openDB();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        tx.objectStore(STORE_NAME).put(value, key);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

// load one Service Worker state value
async function loadDBState(key) {
    const db = await openDB();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readonly");
        const req = tx.objectStore(STORE_NAME).get(key);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

// lazily load the durable Service Worker mapping state
function getState() {
    if (state) return Promise.resolve(state);

    stateLoadPromise ??= loadDBState("state").then(persistedState => {
        state = persistedState || { rules: [] };
        return state;
    });

    return stateLoadPromise;
}

// normalize one virtual-to-physical asset mapping
function normalizeRule(rule) {
    if (!rule || typeof rule.src !== "string" || !rule.src.trim() || typeof rule.dst !== "string" || !rule.dst.trim()) {
        throw new Error("Service Worker mappings require non-empty src and dst values.");
    }

    const src = new URL(rule.src, self.location.origin);
    src.search = "";
    src.hash = "";
    src.pathname = src.pathname.replace(/\/+$/, "") || "/";

    return { ...rule, src: src.href };
}

// add mappings while preserving immutable source-to-destination ownership
async function registerMappings(rules) {
    if (!Array.isArray(rules)) throw new Error("Service Worker mappings must be an array.");

    const currentState = await getState();
    const candidateRules = currentState.rules.map(normalizeRule);
    const rulesBySrc = new Map(candidateRules.map(rule => [rule.src, rule]));

    // merge new mappings into the existing durable registry
    for (const incomingRule of rules.map(normalizeRule)) {
        const registeredRule = rulesBySrc.get(incomingRule.src);

        if (registeredRule) {
            if (registeredRule.dst !== incomingRule.dst) {
                throw new Error(`Service Worker mapping '${incomingRule.src}' is already registered to '${registeredRule.dst}' and cannot be remapped to '${incomingRule.dst}'.`);
            }
            continue;
        }

        candidateRules.push(incomingRule);
        rulesBySrc.set(incomingRule.src, incomingRule);
    }

    // persist the complete candidate registry before publishing it in memory
    const candidateState = { ...currentState, rules: candidateRules };
    await saveDBState("state", candidateState);
    state = candidateState;
}

// serialize mapping registrations to avoid concurrent state updates
function enqueueMappingRegistration(rules) {
    const registration = registrationQueue.then(() => registerMappings(rules));
    registrationQueue = registration.catch(() => {});
    return registration;
}

// activate the new Service Worker immediately after installation
self.addEventListener("install", event => {
    event.waitUntil(self.skipWaiting());
    console.log("sw: installing ...");
});

// claim existing application clients after activation
self.addEventListener("activate", event => {
    event.waitUntil(self.clients.claim());
    console.log("sw: activated");
});

// receive and persist mapping registrations from the application
self.addEventListener("message", event => {
    event.waitUntil((async () => {
        console.log("sw: received message:", event.data);
        const port = event.ports?.[0];

        // reject unsupported Service Worker message types
        if (event.data?.type !== "registerMappings") {
            port?.postMessage({ type: "error", message: `Unsupported Service Worker message '${event.data?.type}'.` });
            return;
        }

        // register mappings and acknowledge completion
        try {
            await enqueueMappingRegistration(event.data.payload?.rules);
            port?.postMessage({ type: "registered" });
        } catch (error) {
            port?.postMessage({ type: "error", message: error?.message || String(error) });
        }
    })());
});

// intercept GET and HEAD requests after mapping registration has settled
self.addEventListener("fetch", event => {
    if (event.request.method !== "GET" && event.request.method !== "HEAD") return;

    event.respondWith((async () => {
        await registrationQueue;
        await getState();
        return handleRequest(event.request);
    })());
});

// resolve a virtual application request through the registered mappings
async function handleRequest(request) {
    const debug = false;

    // bypass requests outside the Service Worker scope
    if (!request.url.startsWith(self.registration.scope)) {
        if (debug) console.log("sw: request outside scope: " + request.url);
        return fetch(request);
    }

    // bypass requests when no mappings have been registered
    if (!state?.rules?.length) {
        if (debug) console.log("sw: request no rules, just fetch: " + request.url);
        return fetch(request);
    }

    const requestUrl = new URL(request.url);
    let rule = null;
    let ruleSrcUrl = null;

    // select the longest matching virtual asset mapping
    for (const targetRule of state.rules) {
        const srcUrl = new URL(targetRule.src, self.location.origin);

        if (requestUrl.origin === srcUrl.origin &&
            (requestUrl.pathname === srcUrl.pathname || requestUrl.pathname.startsWith(srcUrl.pathname + "/"))) {
            if (!ruleSrcUrl || srcUrl.pathname.length > ruleSrcUrl.pathname.length) {
                rule = targetRule;
                ruleSrcUrl = srcUrl;
            }
        }
    }

    // bypass requests that do not belong to a registered mapping
    if (!rule) {
        if (debug) console.log("sw: no matching rule: " + request.url);
        return fetch(request);
    }

    // delegate package-backed destinations to a matching asset handler
    const assetHandler = assetHandlers.find(handler => handler.canHandle(rule.dst));
    if (assetHandler) return assetHandler.handleRequest(request, rule, ruleSrcUrl, debug);

    // resolve ordinary directory-backed destinations
    const relativePath = requestUrl.pathname.substring(ruleSrcUrl.pathname.length).replace(/^\/+/, "");
    return handleDirectoryRequest(request, rule, relativePath, requestUrl, debug);
}

// proxy a virtual request to a directory-backed physical asset location
async function handleDirectoryRequest(request, rule, relativePath, requestUrl, debug) {
    const baseUrl = rule.dst.endsWith("/") ? rule.dst : rule.dst + "/";
    const url = new URL(relativePath, baseUrl);

    // preserve the virtual request query string
    url.search = requestUrl.search;

    // fetch the corresponding physical resource
    if (debug) console.log("sw: fetching: " + request.url + " --> " + url.toString());

    const response = await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.method !== "GET" && request.method !== "HEAD" ? request.body : undefined
    });

    // prevent physical resource URLs from leaking through redirect headers
    const headers = new Headers(response.headers);
    headers.delete("Location");
    headers.delete("Content-Location");

    // return the physical response through the virtual URL
    return new Response(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers
    });
}