// state
let state = null;
let stateLoadPromise = null;
let registrationQueue = Promise.resolve();
const zipPromises = new Map();

// utils
const DB_NAME = "xshell-sw" + self.location.pathname.substring(0, self.location.pathname.lastIndexOf("/")).replaceAll("/", "-");
const DB_VERSION = 1;
const STORE_NAME = "state";
const ZIP_CACHE_NAME = "xshell-sw-packages" + self.location.pathname.substring(0, self.location.pathname.lastIndexOf("/")).replaceAll("/", "-");
const ZIP_EOCD_SIGNATURE = 0x06054b50;
const ZIP_CENTRAL_HEADER_SIGNATURE = 0x02014b50;
const ZIP_LOCAL_HEADER_SIGNATURE = 0x04034b50;
const ZIP64_EXTRA_FIELD_ID = 0x0001;
const ZIP_MAX_COMMENT_LENGTH = 0xffff;
const ZIP_MAX_ENTRY_SIZE = 256 * 1024 * 1024;
const CRC32_TABLE = createCrc32Table();
function openDB() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = () => {
            const db = req.result;
            if (!db.objectStoreNames.contains(STORE_NAME)) {
                db.createObjectStore(STORE_NAME);
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}
async function saveDBState(key, value) {
    const db = await openDB();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_NAME, "readwrite");
        tx.objectStore(STORE_NAME).put(value, key);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}
async function loadDBState(key) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readonly");
    const req = tx.objectStore(STORE_NAME).get(key);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}
function getState() {
    if (state) return Promise.resolve(state);
    stateLoadPromise ??= loadDBState("state").then(persistedState => {
        state = persistedState || { rules: [] };
        return state;
    });
    return stateLoadPromise;
}
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
async function registerMappings(rules) {
    if (!Array.isArray(rules)) throw new Error("Service Worker mappings must be an array.");

    // build and validate the complete candidate registry before changing durable or in-memory state
    const currentState = await getState();
    const candidateRules = currentState.rules.map(normalizeRule);
    const rulesBySrc = new Map(candidateRules.map(rule => [rule.src, rule]));
    for (const incomingRule of rules.map(normalizeRule)) {
        const registeredRule = rulesBySrc.get(incomingRule.src);
        if (registeredRule) {
            if (registeredRule.dst !== incomingRule.dst) {
                throw new Error(
                    `Service Worker mapping '${incomingRule.src}' is already registered to '${registeredRule.dst}' ` +
                    `and cannot be remapped to '${incomingRule.dst}'.`
                );
            }
            continue;
        }
        candidateRules.push(incomingRule);
        rulesBySrc.set(incomingRule.src, incomingRule);
    }

    const candidateState = { ...currentState, rules: candidateRules };
    await saveDBState("state", candidateState);
    state = candidateState;
}
function enqueueMappingRegistration(rules) {
    const registration = registrationQueue.then(() => registerMappings(rules));
    registrationQueue = registration.catch(() => {});
    return registration;
}


// events
self.addEventListener("install", (event) => {
    event.waitUntil(self.skipWaiting()); // move from waiting -> active asap
    console.log("sw: installing ...");
});
self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim()); // start controlling open pages
    console.log("sw: activated");
});
self.addEventListener("message", (event) => {
    event.waitUntil((async () => {
        console.log("sw: received message:", event.data);
        const port = event.ports?.[0];
        if (event.data?.type !== "registerMappings") {
            port?.postMessage({ type: "error", message: `Unsupported Service Worker message '${event.data?.type}'.` });
            return;
        }
        try {
            await enqueueMappingRegistration(event.data.payload?.rules);
            port?.postMessage({ type: "registered" });
        } catch (error) {
            port?.postMessage({ type: "error", message: error?.message || String(error) });
        }
    })());
});
self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET" && event.request.method !== "HEAD") {
        return;
    }
    event.respondWith((async () => {
        await registrationQueue;
        await getState();
        return handleRequest(event.request);
    })());
});


// methods
async function handleRequest(request) {

    // debug
    const debug = false;
    
    // if request is outside scope, just fetch
    if (!request.url.startsWith(self.registration.scope)) {
        if (debug) console.log("sw: request outside scope: " + request.url);
        return fetch(request);
    }

    // if no rules, just fetch
    if (!state?.rules?.length) {
        if (debug) console.log("sw: request no rules, just fetch: " + request.url);
        return fetch(request);
    }

    // create URL object for the request
    const requestUrl = new URL(request.url);

    // determine the rule to use
    let rule = null;
    let ruleSrcUrl = null;
    for (const targetRule of state.rules) {
        const srcUrl = new URL(targetRule.src, self.location.origin);
        if (requestUrl.origin === srcUrl.origin && (requestUrl.pathname === srcUrl.pathname || requestUrl.pathname.startsWith(srcUrl.pathname + "/"))) {
            if (!ruleSrcUrl || srcUrl.pathname.length > ruleSrcUrl.pathname.length) {
                rule = targetRule;
                ruleSrcUrl = srcUrl;
            }
        }
    }

    // no matching rule    
    if (!rule) {
        if (debug) console.log("sw: no matching rule: " + request.url)
        return fetch(request);
    }

    // resolve the configured virtual asset path against the physical assets URL
    const relativePath = requestUrl.pathname.substring(ruleSrcUrl.pathname.length).replace(/^\/+/, "");
    if (isZipUrl(rule.dst)) {
        return handleZipRequest(request, rule, ruleSrcUrl, debug);
    }
    return handleDirectoryRequest(request, rule, relativePath, requestUrl, debug);
}
async function handleDirectoryRequest(request, rule, relativePath, requestUrl, debug) {
    const baseUrl = rule.dst.endsWith("/") ? rule.dst : rule.dst + "/";
    const url = new URL(relativePath, baseUrl);

    // preserve query string
    url.search = requestUrl.search;

    // fetch the real resource
    if (debug) console.log("sw: fetching: " + request.url + " --> " + url.toString());
    const response = await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.method !== "GET" && request.method !== "HEAD"
            ? request.body
            : undefined,
        //mode: "same-origin",
        //credentials: "same-origin",
        //redirect: "manual"
    });

    // prevent physical resource URL from leaking through redirect-related headers
    const headers = new Headers(response.headers);
    headers.delete("Location");
    headers.delete("Content-Location");

    return new Response(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers
    });
}
async function handleZipRequest(request, rule, ruleSrcUrl, debug) {
    let entryName;
    try {
        const pathSuffix = new URL(request.url).pathname.substring(ruleSrcUrl.pathname.length);
        const relativePath = pathSuffix.startsWith("/") ? pathSuffix.substring(1) : pathSuffix;
        entryName = normalizeZipEntryPath(relativePath);
    } catch (error) {
        if (debug) console.log("sw: invalid ZIP entry path: " + error.message);
        return new Response(null, { status: 400, statusText: "Bad Request" });
    }

    try {
        const zipUrl = new URL(rule.dst, self.location.origin);
        zipUrl.hash = "";
        const zip = await loadZip(zipUrl.href);
        const entry = zip.entries.get(entryName);
        if (!entry) {
            return new Response(null, { status: 404, statusText: "Not Found" });
        }

        // extract for both GET and HEAD so size and CRC validation remain identical
        const body = await extractZipEntry(zip, entry);
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
function isZipUrl(url) {
    return new URL(url, self.location.origin).pathname.toLowerCase().endsWith(".zip");
}
function normalizeZipEntryPath(relativePath) {
    let decodedPath;
    try {
        decodedPath = decodeURIComponent(relativePath);
    } catch {
        throw new Error("ZIP entry path is not valid percent-encoding.");
    }
    if (!decodedPath || decodedPath.startsWith("/") || decodedPath.includes("\\")) {
        throw new Error("ZIP entry path is not package-root-relative.");
    }
    const segments = decodedPath.split("/");
    if (segments.some(segment => !segment || segment === "." || segment === "..")) {
        throw new Error("ZIP entry path contains an invalid segment.");
    }
    return segments.join("/");
}
async function loadZip(url) {
    if (zipPromises.has(url)) return zipPromises.get(url);

    const promise = (async () => {
        const cache = await caches.open(ZIP_CACHE_NAME);
        let response = await cache.match(url);
        if (!response) {
            response = await fetch(url);
            if (!response.ok) throw new Error("ZIP package could not be loaded.");
            await cache.put(url, response.clone());
        } else if (!response.ok) {
            throw new Error("Cached ZIP package is not a successful response.");
        }
        return parseZip(new Uint8Array(await response.arrayBuffer()));
    })();
    zipPromises.set(url, promise);
    try {
        return await promise;
    } catch (error) {
        if (zipPromises.get(url) === promise) zipPromises.delete(url);
        throw error;
    }
}
function parseZip(bytes) {
    if (!(bytes instanceof Uint8Array)) throw new Error("ZIP package bytes are invalid.");
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const eocdOffset = findZipEocd(view, bytes.byteLength);
    const diskNumber = view.getUint16(eocdOffset + 4, true);
    const centralDirectoryDisk = view.getUint16(eocdOffset + 6, true);
    const entriesOnDisk = view.getUint16(eocdOffset + 8, true);
    const entryCount = view.getUint16(eocdOffset + 10, true);
    const centralDirectorySize = view.getUint32(eocdOffset + 12, true);
    const centralDirectoryOffset = view.getUint32(eocdOffset + 16, true);
    if (diskNumber === 0xffff || centralDirectoryDisk === 0xffff || entriesOnDisk === 0xffff || entryCount === 0xffff ||
        centralDirectorySize === 0xffffffff || centralDirectoryOffset === 0xffffffff) {
        throw new Error("ZIP64 archives are not supported.");
    }
    if (diskNumber !== 0 || centralDirectoryDisk !== 0 || entriesOnDisk !== entryCount) {
        throw new Error("Multi-disk ZIP archives are not supported.");
    }
    ensureZipRange(eocdOffset, centralDirectoryOffset, centralDirectorySize, "ZIP central directory is out of bounds.");

    const entries = new Map();
    const entryNames = new Set();
    const centralDirectoryEnd = centralDirectoryOffset + centralDirectorySize;
    let offset = centralDirectoryOffset;
    for (let index = 0; index < entryCount; index++) {
        ensureZipRange(centralDirectoryEnd, offset, 46, "ZIP central directory header is truncated.");
        if (view.getUint32(offset, true) !== ZIP_CENTRAL_HEADER_SIGNATURE) throw new Error("ZIP central directory signature is invalid.");
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
        if (compressedSize === 0xffffffff || uncompressedSize === 0xffffffff || localHeaderOffset === 0xffffffff || diskStart === 0xffff) {
            throw new Error("ZIP64 archives are not supported.");
        }
        if (diskStart !== 0) throw new Error("Multi-disk ZIP archives are not supported.");
        if ((flags & 0x0001) !== 0) throw new Error("Encrypted ZIP entries are not supported.");
        if (compressionMethod !== 0 && compressionMethod !== 8) throw new Error(`ZIP compression method ${compressionMethod} is not supported.`);
        if (uncompressedSize > ZIP_MAX_ENTRY_SIZE) throw new Error("ZIP entry exceeds the maximum extracted size.");

        const variableLength = fileNameLength + extraFieldLength + commentLength;
        ensureZipRange(centralDirectoryEnd, offset + 46, variableLength, "ZIP central directory entry is truncated.");
        const fileNameStart = offset + 46;
        const extraFieldStart = fileNameStart + fileNameLength;
        if (hasZipExtraField(view, centralDirectoryEnd, extraFieldStart, extraFieldLength, ZIP64_EXTRA_FIELD_ID)) {
            throw new Error("ZIP64 archives are not supported.");
        }
        const entryName = decodeZipFileName(bytes.subarray(fileNameStart, fileNameStart + fileNameLength), flags);
        validateZipArchiveEntryName(entryName);
        if (entryNames.has(entryName)) throw new Error(`ZIP archive contains duplicate entry '${entryName}'.`);
        entryNames.add(entryName);
        if (!entryName.endsWith("/")) {
            entries.set(entryName, {
                entryName, compressionMethod, flags, crc, compressedSize, uncompressedSize, localHeaderOffset, centralDirectoryOffset
            });
        }
        offset += 46 + variableLength;
    }
    if (offset !== centralDirectoryEnd) throw new Error("ZIP central directory size is invalid.");
    return { bytes, entries };
}
function findZipEocd(view, byteLength) {
    if (byteLength < 22) throw new Error("ZIP end of central directory record is missing.");
    const minimumOffset = Math.max(0, byteLength - 22 - ZIP_MAX_COMMENT_LENGTH);
    for (let offset = byteLength - 22; offset >= minimumOffset; offset--) {
        if (view.getUint32(offset, true) !== ZIP_EOCD_SIGNATURE) continue;
        const commentLength = view.getUint16(offset + 20, true);
        if (offset + 22 + commentLength === byteLength) return offset;
    }
    throw new Error("ZIP end of central directory record is missing.");
}
function hasZipExtraField(view, limit, offset, length, requestedId) {
    const end = offset + length;
    ensureZipRange(limit, offset, length, "ZIP extra field is out of bounds.");
    while (offset < end) {
        ensureZipRange(end, offset, 4, "ZIP extra field header is truncated.");
        const id = view.getUint16(offset, true);
        const fieldLength = view.getUint16(offset + 2, true);
        offset += 4;
        ensureZipRange(end, offset, fieldLength, "ZIP extra field data is truncated.");
        if (id === requestedId) return true;
        offset += fieldLength;
    }
    return false;
}
function decodeZipFileName(bytes, flags) {
    try {
        // Pack emits UTF-8 names; ASCII-compatible names are decoded identically when the UTF-8 flag is absent
        return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
    } catch {
        throw new Error((flags & 0x0800) !== 0 ? "ZIP entry has an invalid UTF-8 filename." : "ZIP entry filename is not UTF-8 compatible.");
    }
}
function validateZipArchiveEntryName(entryName) {
    if (!entryName || entryName.startsWith("/") || entryName.includes("\\")) throw new Error("ZIP archive contains an unsafe entry name.");
    const path = entryName.endsWith("/") ? entryName.substring(0, entryName.length - 1) : entryName;
    const segments = path.split("/");
    if (segments.some(segment => !segment || segment === "." || segment === "..")) throw new Error("ZIP archive contains an unsafe entry name.");
}
async function extractZipEntry(zip, entry) {
    const view = new DataView(zip.bytes.buffer, zip.bytes.byteOffset, zip.bytes.byteLength);
    ensureZipRange(entry.centralDirectoryOffset, entry.localHeaderOffset, 30, "ZIP local file header is out of bounds.");
    if (view.getUint32(entry.localHeaderOffset, true) !== ZIP_LOCAL_HEADER_SIGNATURE) throw new Error("ZIP local file header signature is invalid.");
    const localFlags = view.getUint16(entry.localHeaderOffset + 6, true);
    const localCompressionMethod = view.getUint16(entry.localHeaderOffset + 8, true);
    const fileNameLength = view.getUint16(entry.localHeaderOffset + 26, true);
    const extraFieldLength = view.getUint16(entry.localHeaderOffset + 28, true);
    if ((localFlags & 0x0001) !== 0) throw new Error("Encrypted ZIP entries are not supported.");
    if (localFlags !== entry.flags || localCompressionMethod !== entry.compressionMethod) {
        throw new Error("ZIP local and central file headers do not match.");
    }

    const fileNameStart = entry.localHeaderOffset + 30;
    ensureZipRange(entry.centralDirectoryOffset, fileNameStart, fileNameLength + extraFieldLength, "ZIP local file header is truncated.");
    if (hasZipExtraField(view, entry.centralDirectoryOffset, fileNameStart + fileNameLength, extraFieldLength, ZIP64_EXTRA_FIELD_ID)) {
        throw new Error("ZIP64 archives are not supported.");
    }
    const localEntryName = decodeZipFileName(zip.bytes.subarray(fileNameStart, fileNameStart + fileNameLength), localFlags);
    if (localEntryName !== entry.entryName) throw new Error("ZIP local and central filenames do not match.");
    const dataStart = fileNameStart + fileNameLength + extraFieldLength;
    ensureZipRange(entry.centralDirectoryOffset, dataStart, entry.compressedSize, "ZIP entry data is out of bounds.");
    const compressedBytes = zip.bytes.subarray(dataStart, dataStart + entry.compressedSize);
    const extractedBytes = entry.compressionMethod === 0 ? compressedBytes.slice() : await inflateRaw(compressedBytes, entry.uncompressedSize);
    if (extractedBytes.byteLength !== entry.uncompressedSize) {
        throw new Error("ZIP entry uncompressed size does not match its central directory record.");
    }
    if (crc32(extractedBytes) !== entry.crc) throw new Error("ZIP entry CRC32 validation failed.");
    return extractedBytes;
}
async function inflateRaw(bytes, expectedSize) {
    const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("deflate-raw"));
    const reader = stream.getReader();
    const chunks = [];
    let length = 0;
    try {
        while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            length += value.byteLength;
            if (length > expectedSize || length > ZIP_MAX_ENTRY_SIZE) throw new Error("ZIP entry expands beyond its declared size.");
            chunks.push(value);
        }
    } catch (error) {
        await reader.cancel(error).catch(() => {});
        throw error;
    }
    const result = new Uint8Array(length);
    let offset = 0;
    for (const chunk of chunks) {
        result.set(chunk, offset);
        offset += chunk.byteLength;
    }
    return result;
}
function getMimeType(entryName) {
    const extensionIndex = entryName.lastIndexOf(".");
    const extension = extensionIndex < 0 ? "" : entryName.substring(extensionIndex).toLowerCase();
    return ({
        ".js": "text/javascript", ".mjs": "text/javascript", ".css": "text/css", ".json": "application/json", ".jsonc": "application/json",
        ".html": "text/html", ".htm": "text/html", ".svg": "image/svg+xml", ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
        ".webp": "image/webp", ".gif": "image/gif", ".ico": "image/x-icon", ".woff": "font/woff", ".woff2": "font/woff2", ".ttf": "font/ttf",
        ".otf": "font/otf", ".txt": "text/plain", ".md": "text/markdown", ".xml": "application/xml", ".wasm": "application/wasm"
    })[extension] || "application/octet-stream";
}
function createCrc32Table() {
    const table = new Uint32Array(256);
    for (let index = 0; index < table.length; index++) {
        let value = index;
        for (let bit = 0; bit < 8; bit++) value = (value & 1) !== 0 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
        table[index] = value >>> 0;
    }
    return table;
}
function crc32(bytes) {
    let value = 0xffffffff;
    for (const byte of bytes) value = CRC32_TABLE[(value ^ byte) & 0xff] ^ (value >>> 8);
    return (value ^ 0xffffffff) >>> 0;
}
function ensureZipRange(limit, offset, length, message) {
    if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(length) || offset < 0 || length < 0 || offset > limit || length > limit - offset) {
        throw new Error(message);
    }
}
