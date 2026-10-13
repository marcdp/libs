import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";
import { deflateRawSync } from "node:zlib";

const workerPath = new URL("../sw.js", import.meta.url);
const workerSource = readFileSync(workerPath, "utf8");
const packageUrl = "https://cdn.test/orders/1.0.0.hash/module.zip";
const virtualRoot = "https://example.test/app/_assets/orders/1.0.0.hash";

function clone(value) {
    return value === undefined ? undefined : JSON.parse(JSON.stringify(value));
}

function createIndexedDB(storage) {
    return {
        open() {
            const request = {};
            queueMicrotask(() => {
                const database = {
                    objectStoreNames: { contains() { return true; } },
                    createObjectStore() {},
                    transaction() {
                        const transaction = { oncomplete: null, onerror: null, error: null };
                        transaction.objectStore = () => ({
                            put(value, key) {
                                queueMicrotask(() => {
                                    storage.set(key, clone(value));
                                    transaction.oncomplete?.();
                                });
                            },
                            get(key) {
                                const getRequest = {};
                                queueMicrotask(() => {
                                    getRequest.result = clone(storage.get(key));
                                    getRequest.onsuccess?.();
                                });
                                return getRequest;
                            }
                        });
                        return transaction;
                    }
                };
                request.result = database;
                request.onupgradeneeded?.();
                request.onsuccess?.();
            });
            return request;
        }
    };
}

function createCacheStorage(storage = new Map()) {
    return {
        storage,
        async open(name) {
            if (!storage.has(name)) storage.set(name, new Map());
            const entries = storage.get(name);
            return {
                async match(requestOrUrl) {
                    const response = entries.get(cacheKey(requestOrUrl));
                    return response?.clone();
                },
                async put(requestOrUrl, response) {
                    entries.set(cacheKey(requestOrUrl), response.clone());
                }
            };
        }
    };
}

function cacheKey(requestOrUrl) {
    return requestOrUrl instanceof Request ? requestOrUrl.url : String(requestOrUrl);
}

function createWorker({ archive = createArchive(), cacheStorage = createCacheStorage(), networkFetch } = {}) {
    const listeners = new Map();
    const indexedDBStorage = new Map();
    const fetches = [];
    const self = {
        location: { origin: "https://example.test", pathname: "/app/sw.js" },
        registration: { scope: "https://example.test/app/" },
        clients: { async claim() {} },
        async skipWaiting() {},
        addEventListener(type, listener) { listeners.set(type, listener); }
    };
    const fetchImplementation = networkFetch || (async input => {
        const url = input instanceof Request ? input.url : String(input);
        return url === packageUrl ? new Response(archive, { status: 200 }) : new Response("directory", { status: 200 });
    });
    const context = vm.createContext({
        URL, Headers, Request, Response, Blob, DecompressionStream, TextDecoder, queueMicrotask,
        console: { log() {} },
        indexedDB: createIndexedDB(indexedDBStorage),
        caches: cacheStorage,
        self,
        async fetch(input, init) {
            fetches.push(input instanceof Request ? input.url : String(input));
            return fetchImplementation(input, init);
        }
    });
    new vm.Script(workerSource, { filename: workerPath.pathname }).runInContext(context);
    new vm.Script(
        "globalThis.__parseCount = 0; globalThis.__parseZip = parseZip; parseZip = (...args) => { __parseCount++; return __parseZip(...args); };"
    ).runInContext(context);

    return {
        cacheStorage,
        context,
        fetches,
        get parseCount() { return context.__parseCount; },
        clearZipMemory() { new vm.Script("zipPromises.clear();").runInContext(context); },
        normalizePath(path) { return new vm.Script(`normalizeZipEntryPath(${JSON.stringify(path)})`).runInContext(context); },
        async register(rules = [{ src: virtualRoot, dst: packageUrl }]) {
            let waiting;
            let reply;
            listeners.get("message")({
                data: { type: "registerMappings", payload: { rules } },
                ports: [{ postMessage(message) { reply = clone(message); } }],
                waitUntil(promise) { waiting = promise; }
            });
            await waiting;
            return reply;
        },
        async request(path, method = "GET") {
            let responsePromise;
            const url = path.startsWith("https:") ? path : virtualRoot + "/" + path;
            listeners.get("fetch")({
                request: new Request(url, { method }),
                respondWith(promise) { responsePromise = promise; }
            });
            return responsePromise;
        }
    };
}

function crc32(bytes) {
    let value = 0xffffffff;
    for (const byte of bytes) {
        value ^= byte;
        for (let bit = 0; bit < 8; bit++) value = (value & 1) !== 0 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
    }
    return (value ^ 0xffffffff) >>> 0;
}

function createArchive(entries = [
    { name: "pages/index.js", content: "export default 'orders';", method: 8 },
    { name: "styles/index.css", content: "body { color: green; }", method: 8 },
    { name: "data/config.json", content: "{\"enabled\":true}", method: 0 },
    { name: "data/blob.unknown", content: "opaque", method: 0 }
], options = {}) {
    const localParts = [];
    const centralParts = [];
    let localOffset = 0;
    for (const definition of entries) {
        const name = Buffer.from(definition.name, "utf8");
        const content = Buffer.from(definition.content);
        const method = definition.method ?? 8;
        const compressed = definition.compressed || (method === 8 ? deflateRawSync(content) : content);
        const checksum = definition.crc ?? crc32(content);
        const flags = definition.flags ?? 0x0800;
        const localFlags = definition.localFlags ?? flags;
        const localMethod = definition.localMethod ?? method;
        const centralFlags = definition.centralFlags ?? flags;
        const centralMethod = definition.centralMethod ?? method;

        const localHeader = Buffer.alloc(30);
        localHeader.writeUInt32LE(0x04034b50, 0);
        localHeader.writeUInt16LE(20, 4);
        localHeader.writeUInt16LE(localFlags, 6);
        localHeader.writeUInt16LE(localMethod, 8);
        localHeader.writeUInt32LE(definition.dataDescriptor ? 0 : checksum, 14);
        localHeader.writeUInt32LE(definition.dataDescriptor ? 0 : compressed.length, 18);
        localHeader.writeUInt32LE(definition.dataDescriptor ? 0 : content.length, 22);
        localHeader.writeUInt16LE(name.length, 26);
        const descriptor = definition.dataDescriptor ? Buffer.alloc(16) : Buffer.alloc(0);
        if (definition.dataDescriptor) {
            descriptor.writeUInt32LE(0x08074b50, 0);
            descriptor.writeUInt32LE(checksum, 4);
            descriptor.writeUInt32LE(compressed.length, 8);
            descriptor.writeUInt32LE(content.length, 12);
        }
        localParts.push(localHeader, name, compressed, descriptor);

        const centralHeader = Buffer.alloc(46);
        centralHeader.writeUInt32LE(0x02014b50, 0);
        centralHeader.writeUInt16LE(20, 4);
        centralHeader.writeUInt16LE(20, 6);
        centralHeader.writeUInt16LE(centralFlags, 8);
        centralHeader.writeUInt16LE(centralMethod, 10);
        centralHeader.writeUInt32LE(checksum, 16);
        centralHeader.writeUInt32LE(compressed.length, 20);
        centralHeader.writeUInt32LE(content.length, 24);
        centralHeader.writeUInt16LE(name.length, 28);
        centralHeader.writeUInt32LE(localOffset, 42);
        centralParts.push(centralHeader, name);
        localOffset += localHeader.length + name.length + compressed.length + descriptor.length;
    }

    const centralDirectory = Buffer.concat(centralParts);
    const eocd = Buffer.alloc(22);
    eocd.writeUInt32LE(0x06054b50, 0);
    eocd.writeUInt16LE(options.zip64 ? 0xffff : entries.length, 8);
    eocd.writeUInt16LE(options.zip64 ? 0xffff : entries.length, 10);
    eocd.writeUInt32LE(centralDirectory.length, 12);
    eocd.writeUInt32LE(localOffset, 16);
    return Buffer.concat([...localParts, centralDirectory, eocd]);
}

test("ZIP mappings extract DEFLATE JavaScript and CSS and stored JSON with MIME types", async () => {
    const worker = createWorker();
    await worker.register();

    const javascript = await worker.request("pages/index.js");
    assert.equal(javascript.status, 200);
    assert.equal(javascript.headers.get("Content-Type"), "text/javascript");
    assert.equal(javascript.headers.get("Content-Length"), String(Buffer.byteLength("export default 'orders';")));
    assert.equal(await javascript.text(), "export default 'orders';");

    const css = await worker.request("styles/index.css");
    assert.equal(css.headers.get("Content-Type"), "text/css");
    assert.equal(await css.text(), "body { color: green; }");

    const stored = await worker.request("data/config.json");
    assert.equal(stored.headers.get("Content-Type"), "application/json");
    assert.equal(await stored.text(), "{\"enabled\":true}");
    assert.deepEqual(worker.fetches, [packageUrl]);
    assert.equal(worker.parseCount, 1);
});

test("ZIP lookup ignores queries, supplies the fallback MIME type, and implements HEAD", async () => {
    const worker = createWorker();
    await worker.register();

    const queried = await worker.request("pages/index.js?v=123");
    assert.equal(await queried.text(), "export default 'orders';");
    const unknown = await worker.request("data/blob.unknown");
    assert.equal(unknown.headers.get("Content-Type"), "application/octet-stream");
    const head = await worker.request("styles/index.css", "HEAD");
    assert.equal(head.status, 200);
    assert.equal(head.headers.get("Content-Type"), "text/css");
    assert.equal(head.headers.get("Content-Length"), String(Buffer.byteLength("body { color: green; }")));
    assert.equal(await head.text(), "");
});

test("missing ZIP entries return 404 without falling through to the virtual request", async () => {
    const worker = createWorker();
    await worker.register();
    const response = await worker.request("pages/missing.js");
    assert.equal(response.status, 404);
    assert.deepEqual(worker.fetches, [packageUrl]);
});

test("Cache Storage survives an in-memory ZIP index reset without another network fetch", async () => {
    const worker = createWorker();
    await worker.register();
    assert.equal((await worker.request("pages/index.js")).status, 200);
    worker.clearZipMemory();
    assert.equal((await worker.request("styles/index.css")).status, 200);
    assert.deepEqual(worker.fetches, [packageUrl]);
    assert.equal(worker.parseCount, 2);
});

test("concurrent ZIP requests share one load and parse promise", async () => {
    let release;
    const gate = new Promise(resolve => { release = resolve; });
    const archive = createArchive();
    const worker = createWorker({
        archive,
        async networkFetch() {
            await gate;
            return new Response(archive, { status: 200 });
        }
    });
    await worker.register();
    const first = worker.request("pages/index.js");
    const second = worker.request("styles/index.css");
    await new Promise(resolve => setImmediate(resolve));
    assert.deepEqual(worker.fetches, [packageUrl]);
    release();
    assert.deepEqual((await Promise.all([first, second])).map(response => response.status), [200, 200]);
    assert.equal(worker.parseCount, 1);
});

test("failed ZIP loads do not poison the in-memory promise cache", async () => {
    const archive = createArchive();
    let attempts = 0;
    const worker = createWorker({
        archive,
        async networkFetch() {
            attempts++;
            return attempts === 1 ? new Response(null, { status: 503 }) : new Response(archive, { status: 200 });
        }
    });
    await worker.register();
    assert.equal((await worker.request("pages/index.js")).status, 502);
    assert.equal((await worker.request("pages/index.js")).status, 200);
    assert.deepEqual(worker.fetches, [packageUrl, packageUrl]);
    assert.equal(worker.parseCount, 1);
});

test("central-directory sizes support entries that use a data descriptor", async () => {
    const archive = createArchive([{ name: "pages/index.js", content: "descriptor", method: 8, flags: 0x0808, dataDescriptor: true }]);
    const worker = createWorker({ archive });
    await worker.register();
    const response = await worker.request("pages/index.js");
    assert.equal(response.status, 200);
    assert.equal(await response.text(), "descriptor");
});

test("the longest matching mapping remains authoritative when it is ZIP-backed", async () => {
    const worker = createWorker();
    await worker.register([
        { src: "https://example.test/app/_assets/orders", dst: "https://cdn.test/legacy/" },
        { src: virtualRoot, dst: packageUrl }
    ]);
    const response = await worker.request("pages/index.js");
    assert.equal(await response.text(), "export default 'orders';");
    assert.deepEqual(worker.fetches, [packageUrl]);
});

test("directory mappings retain physical URL resolution, queries, methods, and redirect-header stripping", async () => {
    const worker = createWorker({
        async networkFetch() {
            return new Response("directory", { headers: { Location: "physical", "Content-Location": "physical" } });
        }
    });
    const directoryRoot = "https://example.test/app/_assets/dev/1.0.0.dev";
    await worker.register([{ src: directoryRoot, dst: "https://cdn.test/dev/" }]);
    const response = await worker.request(directoryRoot + "/pages/index.js?mode=full", "HEAD");
    assert.deepEqual(worker.fetches, ["https://cdn.test/dev/pages/index.js?mode=full"]);
    assert.equal(response.headers.has("Location"), false);
    assert.equal(response.headers.has("Content-Location"), false);
});

test("malformed, unsupported, encrypted, ZIP64, duplicate, and CRC-invalid archives return controlled 502 responses", async t => {
    const corrupt = createArchive();
    corrupt.writeUInt32LE(0, corrupt.length - 22);
    const cases = [
        ["malformed EOCD", corrupt],
        ["unsupported compression", createArchive([{ name: "pages/index.js", content: "x", method: 0, centralMethod: 99, localMethod: 99 }])],
        ["encrypted entry", createArchive([{ name: "pages/index.js", content: "x", method: 0, flags: 0x0801 }])],
        ["ZIP64 sentinel", createArchive(undefined, { zip64: true })],
        ["duplicate entry", createArchive([{ name: "same.txt", content: "a", method: 0 }, { name: "same.txt", content: "b", method: 0 }])],
        ["CRC mismatch", createArchive([{ name: "pages/index.js", content: "x", method: 0, crc: 0 }])]
    ];
    for (const [name, archive] of cases) {
        await t.test(name, async () => {
            const worker = createWorker({ archive });
            await worker.register();
            const response = await worker.request(name === "duplicate entry" ? "same.txt" : "pages/index.js");
            assert.equal(response.status, 502);
            assert.equal(await response.text(), "");
            assert.equal(worker.fetches.includes(virtualRoot + "/pages/index.js"), false);
        });
    }
});

test("ZIP entry path normalization rejects empty, absolute, dot, traversal, and backslash paths", async () => {
    const worker = createWorker();
    const rejectedPaths = [
        "", "/pages/index.js", "./pages/index.js", "pages/../module.json", "pages\\index.js", "%2Fpages/index.js", "pages/%2e%2e%2Fmodule.json"
    ];
    for (const path of rejectedPaths) {
        assert.throws(() => worker.normalizePath(path));
    }
    assert.equal(worker.normalizePath("pages/index.js"), "pages/index.js");

    await worker.register();
    const traversal = await worker.request("pages/%2e%2e%2Fmodule.json");
    assert.equal(traversal.status, 400);
    assert.deepEqual(worker.fetches, []);
});
