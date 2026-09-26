import assert from "node:assert/strict";
import test from "node:test";

class FakeElement {
    attachShadow() {
        this.shadowRoot = { adoptedStyleSheets: [] };
        return this.shadowRoot;
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = class {
    replaceSync() {}
};
globalThis.customElements = {
    define() {},
    get() { return undefined; }
};
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = { baseURI: "https://example.test/" };

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { createPageClassFromJsDefinition } = await import("../loaders/page-js.js");
const validateComponentContract = (await import("../validation/component.contract.js")).default;
const { default: xshell } = await import("../xshell.js");

class StateEngineFactory {
    constructor(state) {
        this.state = state;
    }

    create() {
        return { ...this.state };
    }
}

class RenderEngineFactory {
    dependencies = [];
    slots = [];

    init() {}
}

function configureDefinitionLoaders() {
    xshell._config = {
        modules: {
            test: {
                defaults: {
                    component: { stateEngine: "test", renderEngine: "test" },
                    page: { stateEngine: "test", renderEngine: "test" }
                }
            }
        }
    };
    xshell._loader = {
        async load(resource) {
            return resource.startsWith("state-engine:") ? StateEngineFactory : RenderEngineFactory;
        }
    };
    xshell._bus = { emit() {} };
}

function createContext() {
    return { resourceDefinition: { moduleId: "test" } };
}

function createDefinition(controller = () => ({})) {
    return { meta: { name: "page-query-test" }, controller };
}

function createContract(properties) {
    return { properties };
}

async function createPage(properties, src, controller = () => ({})) {
    configureDefinitionLoaders();
    const PageClass = await createPageClassFromJsDefinition(src, createContext(), createDefinition(controller), createContract(properties));
    return new PageClass({ src, context: {} });
}

test("Page query binding is explicit and uses kebab-case names", async () => {
    const page = await createPage({
        foo: { type: "string", default: "default", state: true, query: true },
        bar: { type: "string", default: "default", state: true },
        baz: { type: "string", default: "default", state: true, query: false }
    }, "/page.js?foo=queryFoo&bar=queryBar&baz=queryBaz");

    assert.equal(page._state.foo, "queryFoo");
    assert.equal(page._state.bar, "default");
    assert.equal(page._state.baz, "default");

    const camelCasePage = await createPage({
        pageIndex: { type: "integer", default: 0, state: true, query: true },
        id: { type: "string", default: "", state: true, query: true }
    }, "/page.js?page-index=7&id=123");

    assert.equal(camelCasePage._state.pageIndex, 7);
    assert.equal(camelCasePage._state.id, "123");
});

test("Page query values use contract scalar types and retain defaults when absent", async () => {
    const page = await createPage({
        text: { type: "string", default: "default", state: true, query: true },
        scale: { type: "number", default: 1, state: true, query: true },
        pageIndex: { type: "integer", default: 0, state: true, query: true },
        enabled: { type: "boolean", default: false, state: true, query: true },
        nullableNumber: { type: "number", default: null, state: true, query: true },
        missing: { type: "string", default: "retained", state: true, query: true }
    }, "/page.js?text=&scale=1.5&page-index=3&enabled=1&nullable-number=2.5");

    assert.equal(page._state.text, "");
    assert.equal(page._state.scale, 1.5);
    assert.equal(page._state.pageIndex, 3);
    assert.equal(page._state.enabled, true);
    assert.equal(page._state.nullableNumber, 2.5);
    assert.equal(page._state.missing, "retained");

    for (const value of ["true", "1"]) {
        const truePage = await createPage({ enabled: { type: "boolean", default: false, state: true, query: true } }, `/page.js?enabled=${value}`);
        assert.equal(truePage._state.enabled, true);
    }
    for (const value of ["false", "0"]) {
        const falsePage = await createPage({ enabled: { type: "boolean", default: true, state: true, query: true } }, `/page.js?enabled=${value}`);
        assert.equal(falsePage._state.enabled, false);
    }
});

test("Page query conversion rejects malformed scalar values", async () => {
    await assert.rejects(() => createPage({ scale: { type: "number", default: 1, state: true, query: true } }, "/page.js?scale=invalid"), /invalid number query value/);
    await assert.rejects(() => createPage({ pageIndex: { type: "integer", default: 0, state: true, query: true } }, "/page.js?page-index=3.5"), /invalid integer query value/);
    await assert.rejects(() => createPage({ enabled: { type: "boolean", default: false, state: true, query: true } }, "/page.js?enabled="), /invalid boolean query value/);
});

test("Page query metadata requires state-backed supported scalar properties", async () => {
    await assert.rejects(() => createPage({ value: { type: "string", default: "", query: true } }, "/page.js?value=query"), /Page property 'value'.*not state-backed/);
    for (const type of ["object", "array", "function", "any", "void"]) {
        await assert.rejects(() => createPage({ value: { type, default: type === "array" ? [] : null, state: true, query: true } }, "/page.js?value=query"), new RegExp(`Page property 'value'.*unsupported type '${type}'`));
    }
});

test("The controller and load lifecycle see query-derived state", async () => {
    let observedControllerValue;
    let observedLoadValue;
    const page = await createPage({ customerId: { type: "string", default: "default", state: true, query: true } }, "/page.js?customer-id=C42", ({ state }) => ({
        load() {
            observedLoadValue = state.customerId;
        }
    }));

    observedControllerValue = page._state.customerId;
    await page.load();
    assert.equal(observedControllerValue, "C42");
    assert.equal(observedLoadValue, "C42");
});

test("query values are isolated between Page instances", async () => {
    configureDefinitionLoaders();
    const PageClass = await createPageClassFromJsDefinition("/page.js", createContext(), createDefinition(), createContract({
        id: { type: "string", default: "", state: true, query: true }
    }));
    const pageA = new PageClass({ src: "/page.js?id=A", context: {} });
    const pageB = new PageClass({ src: "/page.js?id=B", context: {} });

    assert.equal(pageA._state.id, "A");
    assert.equal(pageB._state.id, "B");
});

test("the shared schema accepts query booleans and rejects other query values", async () => {
    await assert.doesNotReject(() => validateComponentContract("component.js", createContract({ value: { type: "string", query: true } })));
    await assert.doesNotReject(() => validateComponentContract("component.js", createContract({ value: { type: "string", query: false } })));
    await assert.rejects(() => validateComponentContract("component.js", createContract({ value: { type: "string", query: "yes" } })), /Invalid component contract/);
});

test("Components accept query metadata without reading Page URL state", async () => {
    configureDefinitionLoaders();
    const Component = await createComponentClassFromJsDefinition("component.js?value=query", createContext(), createDefinition(), createContract({
        value: { type: "string", default: "default", state: true, query: true }
    }));
    const component = new Component();

    assert.equal(component._state.value, "default");
});
