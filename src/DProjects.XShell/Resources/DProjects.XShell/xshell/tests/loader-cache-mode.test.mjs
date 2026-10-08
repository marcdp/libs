import assert from "node:assert/strict";
import test from "node:test";

globalThis.window = {
    customElements: {
        get() { return undefined; }
    }
};

const { default: Loader } = await import("../loader.js");
const { default: Resolver } = await import("../resolver.js");

const bus = { async emit() {} };
const debug = { log() {}, error() {} };

async function createLoader(testName, { cacheMode, type = "resource" } = {}) {
    // isolate the resource-specific loader state for each test with a distinct module URL
    const loaderUrl = new URL(`./fixtures/counting-loader.mjs?test=${testName}`, import.meta.url).href;
    const fixture = await import(loaderUrl);
    fixture.reset();
    const definition = { url: "/{path}", loader: loaderUrl, cache: true };
    if (cacheMode !== undefined) {
        definition.cacheMode = cacheMode;
    }
    const config = {
        app: { basePath: "", baseUrl: "https://example.test/" },
        xshell: {
            assetsBasePath: "/_assets",
            assetsPath: "/_assets/xshell",
            navigation: { mode: "path", hashPrefix: "#!" },
            ui: { component: { lazy: null } },
            resolver: { [type]: { "/{path}": definition } }
        }
    };
    const resolver = new Resolver({ config });
    return { fixture, loader: new Loader({ bus, config, resolver }) };
}

test("cacheMode defaults to full cache identity", async () => {
    const { fixture, loader } = await createLoader("default-full");
    const first = await loader.load("resource:/foo?a=1");
    const second = await loader.load("resource:/foo?a=2");

    assert.notStrictEqual(first, second);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/foo"]);
});

test("cacheMode full keeps query variants separate", async () => {
    const { fixture, loader } = await createLoader("explicit-full", { cacheMode: "full" });
    const first = await loader.load("resource:/foo?a=1");
    const second = await loader.load("resource:/foo?a=2");

    assert.notStrictEqual(first, second);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/foo"]);
});

test("cacheMode path shares query variants and keeps diagnostics unnormalized", async () => {
    const { fixture, loader } = await createLoader("path", { cacheMode: "path" });
    const first = await loader.load("resource:/foo?a=1");
    const second = await loader.load("resource:/foo?a=2");

    assert.strictEqual(first, second);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo"]);
    assert.equal(loader.registry.length, 1);
    assert.equal(loader.registry[0].resource, "resource:/foo?a=1");
    assert.equal(loader.registry[0].path, "/foo");
    assert.equal(loader.registry[0].url, "https://example.test/foo");
    assert.equal(loader.registry[0].status, "loaded");
    assert.equal(Object.hasOwn(loader.registry[0], "value"), false);
    assert.equal(Object.isFrozen(loader.registry[0]), true);
});

test("successful cached load stores its value for later requests", async () => {
    const { fixture, loader } = await createLoader("resolved-value");
    const first = await loader.load("resource:/foo");

    assert.deepEqual(loader._cache["resource:/foo"], { value: first, promise: null });
    const second = await loader.load("resource:/foo");

    assert.strictEqual(second, first);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo"]);
});

test("failed cached load is evicted and a later request succeeds", async () => {
    const { fixture, loader } = await createLoader("retry-after-failure");
    fixture.failOnce();

    await assert.rejects(() => loader.load("resource:/foo"), /Simulated load failure/);
    assert.equal(loader._cache["resource:/foo"], undefined);
    assert.equal(loader.registry[0].status, "error");

    const value = await loader.load("resource:/foo");

    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/foo"]);
    assert.equal(loader.registry[1].status, "loaded");
    assert.strictEqual(loader._cache["resource:/foo"].value, value);
});

test("failed resource retains its resolved logical path on the error", async t => {
    t.mock.method(console, "error", () => {});
    const { fixture, loader } = await createLoader("error-url");
    fixture.failOnce();

    await assert.rejects(() => loader.load("resource:/foo"), error => {
        assert.equal(error.errors[0].resource, "resource:/foo");
        assert.equal(error.errors[0].path, "/foo");
        assert.equal(Object.hasOwn(error.errors[0], "src"), false);
        return true;
    });
});

test("cacheMode path keeps different paths separate", async () => {
    const { fixture, loader } = await createLoader("different-paths", { cacheMode: "path" });
    const first = await loader.load("resource:/foo?a=1");
    const second = await loader.load("resource:/bar?a=1");

    assert.notStrictEqual(first, second);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/bar"]);
});

test("cacheMode path deduplicates concurrent query variants", async () => {
    const { fixture, loader } = await createLoader("in-flight", { cacheMode: "path" });
    fixture.deferLoads();

    const firstPromise = loader.load("resource:/foo?a=1");
    const secondPromise = loader.load("resource:/foo?a=2");
    await fixture.waitForLoadCount(1);

    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo"]);
    fixture.releaseLoads();
    const [first, second] = await Promise.all([firstPromise, secondPromise]);
    assert.strictEqual(first, second);
});

test("concurrent failures share one load and a later request retries", async () => {
    const { fixture, loader } = await createLoader("shared-failure", { cacheMode: "path" });
    fixture.failOnce();
    fixture.deferLoads();

    const firstPromise = loader.load("resource:/foo?a=1");
    const secondPromise = loader.load("resource:/foo?a=2");
    await fixture.waitForLoadCount(1);

    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo"]);
    fixture.releaseLoads();
    const [first, second] = await Promise.allSettled([firstPromise, secondPromise]);

    assert.equal(first.status, "rejected");
    assert.equal(second.status, "rejected");
    assert.strictEqual(first.reason.errors[0].cause, second.reason.errors[0].cause);
    assert.equal(loader._cache["resource:/foo"], undefined);

    const value = await loader.load("resource:/foo?a=3");

    assert.equal(value.requestNumber, 2);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/foo"]);
    assert.equal(loader.registry.length, 2);
});

test("cacheMode path removes only query and preserves fragment identity", async () => {
    const { fixture, loader } = await createLoader("fragments", { cacheMode: "path" });
    const first = await loader.load("resource:/foo?a=1#one");
    const second = await loader.load("resource:/foo?a=2#one");
    const third = await loader.load("resource:/foo?a=3#two");

    assert.strictEqual(first, second);
    assert.notStrictEqual(first, third);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/foo", "https://example.test/foo"]);
});

test("invalid cacheMode fails clearly", async () => {
    const { fixture, loader } = await createLoader("invalid", { cacheMode: "whatever" });

    await assert.rejects(() => loader.load("resource:/foo?a=1"), /Unsupported cache mode 'whatever'/);
    assert.deepEqual(fixture.getRequests(), []);
});

test("Page path caching reuses implementations while instances retain full src", async () => {
    const { fixture, loader } = await createLoader("page", { cacheMode: "path", type: "page" });
    fixture.usePageClasses();
    const CustomerPageA = await loader.load("page:/customer.js?id=123");
    const CustomerPageB = await loader.load("page:/customer.js?id=456");
    const OrderPage = await loader.load("page:/order.js?id=123");
    const customerA = new CustomerPageA({ src: "/customer.js?id=123" });
    const customerB = new CustomerPageB({ src: "/customer.js?id=456" });

    assert.strictEqual(CustomerPageA, CustomerPageB);
    assert.notStrictEqual(CustomerPageA, OrderPage);
    assert.equal(customerA.src, "/customer.js?id=123");
    assert.equal(customerB.src, "/customer.js?id=456");
    assert.equal(loader.registry[0].description, "Test page");
    assert.equal(Object.hasOwn(loader.registry[0], "value"), false);
    assert.deepEqual(fixture.getRequests(), ["https://example.test/customer.js", "https://example.test/order.js"]);
});
