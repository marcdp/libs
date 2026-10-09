import assert from "node:assert/strict";
import test from "node:test";

const { default: Loader } = await import("../loader.js");

async function createLoader(testName, events, onResolve = () => {}) {
    // isolate each test's concrete loader and capture its resource events
    const loaderUrl = new URL(`./fixtures/counting-loader.mjs?test=timing-${testName}`, import.meta.url).href;
    const fixture = await import(loaderUrl);
    fixture.reset();
    const definition = { loader: loaderUrl, cache: true };
    const resolver = {
        resolve(resource) {
            onResolve(resource);
            return { definition, url: resource.substring("resource:".length), path: resource.substring("resource:".length) };
        }
    };
    const config = {
        app: { basePath: "" },
        xshell: { assetsBasePath: "/_assets", assetsPath: "/_assets/xshell", navigation: { mode: "path" }, ui: { component: { lazy: null } } }
    };
    const bus = { emit(name, detail) { events.push({ name, detail }); } };
    return { fixture, loader: new Loader({ bus, config, resolver }) };
}

test("successful resource time covers only its concrete load and matches the Bus event", async t => {
    let now = 100;
    t.mock.method(performance, "now", () => now);
    const events = [];
    const { fixture, loader } = await createLoader("single", events, () => now += 40);
    fixture.deferLoads();

    const pending = loader.load("resource:/a");
    await fixture.waitForLoadCount(1);
    const snapshot = loader.registry;
    assert.equal(loader.registry[0].status, "pending");
    assert.equal(loader.registry[0].time, null);
    assert.equal(Object.isFrozen(snapshot), true);
    assert.equal(Object.isFrozen(snapshot[0]), true);
    assert.throws(() => { snapshot[0].status = "changed"; }, TypeError);
    now += 27;
    fixture.releaseLoads();
    const first = await pending;

    assert.equal(loader.registry[0].time, 27);
    assert.equal(loader.registry[0].status, "loaded");
    assert.equal(snapshot[0].status, "pending");
    assert.equal(Object.hasOwn(loader.registry[0], "value"), false);
    assert.equal(events[0].name, "xshell:loader:resource:fetch");
    assert.equal(events[1].name, "xshell:loader:resource:loaded");
    assert.equal(events[1].detail.time, loader.registry[0].time);

    // a cache hit keeps the original value and does not create another timed load
    now += 100;
    assert.strictEqual(await loader.load("resource:/a"), first);
    assert.deepEqual(fixture.getRequests(), ["/a"]);
    assert.equal(loader.registry.length, 1);
    assert.equal(events.length, 2);
});

test("resources in one batch have independent timers and remain concurrent", async t => {
    let now = 10;
    t.mock.method(performance, "now", () => now);
    const events = [];
    const { fixture, loader } = await createLoader("batch", events, () => now += 30);
    fixture.deferLoads();

    const pending = loader.load(["resource:/a", "resource:/b", "resource:/c"]);
    await fixture.waitForLoadCount(3);
    assert.deepEqual(fixture.getRequests(), ["/a", "/b", "/c"]);
    assert.deepEqual(loader.registry.map(item => item.status), ["pending", "pending", "pending"]);
    now += 12;
    fixture.releaseLoads();
    await pending;

    assert.deepEqual(loader.registry.map(item => item.time), [72, 42, 12]);
    assert.deepEqual(events.filter(event => event.name === "xshell:loader:resource:loaded").map(event => event.detail.time), [72, 42, 12]);
});

test("failed resource time matches its error event and the failed cache entry is evicted", async t => {
    let now = 20;
    t.mock.method(performance, "now", () => now);
    t.mock.method(console, "error", () => {});
    const events = [];
    const { fixture, loader } = await createLoader("failure", events, () => now += 50);
    fixture.failOnce();
    fixture.deferLoads();

    const pending = loader.load("resource:/a");
    await fixture.waitForLoadCount(1);
    now += 17;
    fixture.releaseLoads();
    await assert.rejects(pending, /Simulated load failure/);

    assert.equal(loader.registry[0].status, "error");
    assert.equal(loader.registry[0].time, 17);
    assert.equal(events[1].name, "xshell:loader:resource:error");
    assert.equal(events[1].detail.time, loader.registry[0].time);
    assert.equal(loader._cache["resource:/a"], undefined);
});
