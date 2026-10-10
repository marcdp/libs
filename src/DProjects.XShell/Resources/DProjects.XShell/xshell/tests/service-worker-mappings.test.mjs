import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";

const workerPath = new URL("../sw.js", import.meta.url);
const workerSource = readFileSync(workerPath, "utf8");

function clone(value) {
    return value === undefined ? undefined : JSON.parse(JSON.stringify(value));
}

function createIndexedDB(storage, events) {
    return {
        open() {
            const request = {};
            queueMicrotask(() => {
                const database = {
                    objectStoreNames: { contains() { return true; } },
                    createObjectStore() {},
                    transaction(storeName, mode) {
                        const transaction = { oncomplete: null, onerror: null, error: null };
                        transaction.objectStore = () => ({
                            put(value, key) {
                                queueMicrotask(() => {
                                    storage.set(key, clone(value));
                                    events.push(`persist:${value.rules.length}`);
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

function createWorker(storage = new Map()) {
    const listeners = new Map();
    const events = [];
    const fetches = [];
    const self = {
        location: { origin: "https://example.test", pathname: "/app/sw.js" },
        registration: { scope: "https://example.test/app/" },
        clients: { async claim() {} },
        async skipWaiting() {},
        addEventListener(type, listener) { listeners.set(type, listener); }
    };
    const context = vm.createContext({
        URL, Headers, Request, Response, queueMicrotask,
        console: { log() {} },
        indexedDB: createIndexedDB(storage, events),
        self,
        async fetch(input) {
            const url = input instanceof Request ? input.url : String(input);
            fetches.push(url);
            return new Response("ok", { status: 200, headers: { Location: "physical", "Content-Location": "physical" } });
        }
    });
    new vm.Script(workerSource, { filename: workerPath.pathname }).runInContext(context);

    return {
        events,
        fetches,
        storage,
        async register(rules) {
            let waiting;
            let reply;
            listeners.get("message")({
                data: { type: "registerMappings", payload: { rules } },
                ports: [{ postMessage(message) { reply = clone(message); events.push(`reply:${message.type}`); } }],
                waitUntil(promise) { waiting = promise; }
            });
            await waiting;
            return reply;
        },
        async fetch(url) {
            let responsePromise;
            listeners.get("fetch")({
                request: new Request(url),
                respondWith(promise) { responsePromise = promise; }
            });
            return responsePromise;
        }
    };
}

function rule(path, dst, extra = {}) {
    return { src: `https://example.test/app${path}`, dst, ...extra };
}

function persistedRules(worker) {
    return worker.storage.get("state")?.rules || [];
}

test("mapping registration is additive, durable before acknowledgement, and idempotent by src and dst", async () => {
    const worker = createWorker();
    const a = rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/orders/1.4.0/");
    const b = rule("/_assets/orders/1.5.0.bbb", "https://cdn.test/orders/1.5.0/");

    assert.deepEqual(await worker.register([a]), { type: "registered" });
    assert.deepEqual(worker.events, ["persist:1", "reply:registered"]);
    assert.deepEqual(await worker.register([b]), { type: "registered" });
    assert.deepEqual(persistedRules(worker).map(item => item.src), [a.src, b.src]);

    assert.deepEqual(await worker.register([{ ...a, src: a.src + "/", name: "different diagnostics" }]), { type: "registered" });
    assert.equal(persistedRules(worker).length, 2);
    assert.equal(persistedRules(worker)[0].name, undefined);
});

test("an immutable conflict rejects the complete batch without changing persisted mappings", async () => {
    const worker = createWorker();
    const a = rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/orders/1.4.0/");
    const b = rule("/_assets/reports/2.0.0.bbb", "https://cdn.test/reports/2.0.0/");
    await worker.register([a]);
    const persistedBefore = clone(persistedRules(worker));

    const reply = await worker.register([b, { ...a, dst: "https://other.test/orders/" }]);
    assert.equal(reply.type, "error");
    assert.match(reply.message, /already registered.*cannot be remapped/);
    assert.deepEqual(persistedRules(worker), persistedBefore);
    assert.equal(persistedRules(worker).some(item => item.src === b.src), false);
    await worker.fetch(a.src + "/pages/index.js");
    await worker.fetch(b.src + "/pages/index.js");
    assert.deepEqual(worker.fetches, ["https://cdn.test/orders/1.4.0/pages/index.js", b.src + "/pages/index.js"]);
});

test("framework and multiple module generations coexist and remain independently routable", async () => {
    const worker = createWorker();
    await worker.register([
        rule("/_assets/xshell/0.9.0.xxx", "https://cdn.test/xshell/"),
        rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/orders/1.4.0/"),
        rule("/_assets/orders/1.5.0.bbb", "https://cdn.test/orders/1.5.0/"),
        rule("/_assets/reports/2.0.0.rrr", "https://cdn.test/reports/")
    ]);

    await worker.fetch("https://example.test/app/_assets/orders/1.4.0.aaa/pages/index.js");
    await worker.fetch("https://example.test/app/_assets/orders/1.5.0.bbb/pages/index.js");
    assert.deepEqual(worker.fetches, [
        "https://cdn.test/orders/1.4.0/pages/index.js",
        "https://cdn.test/orders/1.5.0/pages/index.js"
    ]);
    assert.equal(persistedRules(worker).length, 4);
});

test("a restarted worker restores the complete persisted registry", async () => {
    const storage = new Map();
    const firstWorker = createWorker(storage);
    await firstWorker.register([
        rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/orders/1.4.0/"),
        rule("/_assets/reports/2.0.0.bbb", "https://cdn.test/reports/2.0.0/")
    ]);

    const restartedWorker = createWorker(storage);
    await restartedWorker.fetch("https://example.test/app/_assets/reports/2.0.0.bbb/pages/index.js?mode=full");
    assert.deepEqual(restartedWorker.fetches, ["https://cdn.test/reports/2.0.0/pages/index.js?mode=full"]);
});

test("concurrent registrations are serialized and persist their union", async () => {
    const worker = createWorker();
    const a = rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/orders/1.4.0/");
    const b = rule("/_assets/reports/2.0.0.bbb", "https://cdn.test/reports/2.0.0/");

    const [aReply, bReply] = await Promise.all([worker.register([a]), worker.register([b])]);
    assert.deepEqual(aReply, { type: "registered" });
    assert.deepEqual(bReply, { type: "registered" });
    assert.deepEqual(new Set(persistedRules(worker).map(item => item.src)), new Set([a.src, b.src]));
});

test("routing selects the longest complete-boundary match independently of registration order", async () => {
    const worker = createWorker();
    await worker.register([
        rule("/_assets/orders", "https://cdn.test/legacy/"),
        rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/generation/")
    ]);
    await worker.fetch("https://example.test/app/_assets/orders/1.4.0.aaa/pages/index.js");
    assert.deepEqual(worker.fetches, ["https://cdn.test/generation/pages/index.js"]);

    const boundaryWorker = createWorker();
    await boundaryWorker.register([rule("/_assets/orders/1.4.0.aaa", "https://cdn.test/generation/")]);
    const unrelated = "https://example.test/app/_assets/orders/1.4.0.aaa-extra/pages/index.js";
    await boundaryWorker.fetch(unrelated);
    assert.deepEqual(boundaryWorker.fetches, [unrelated]);
});
