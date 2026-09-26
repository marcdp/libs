import assert from "node:assert/strict";
import test from "node:test";

class FakeElement {
    constructor() {
        this._attributes = new Map();
    }

    attachShadow() {
        this.shadowRoot = { adoptedStyleSheets: [] };
        return this.shadowRoot;
    }

    getAttribute(name) {
        return this._attributes.get(name) ?? null;
    }

    setAttribute(name, value) {
        this._attributes.set(name, String(value));
    }

    removeAttribute(name) {
        this._attributes.delete(name);
    }

    get attributes() {
        return [...this._attributes].map(([name, value]) => ({ name, value }));
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = class {
    replaceSync() {}
};
globalThis.customElements = {
    definitions: new Map(),
    define(name, definition) {
        this.definitions.set(name, definition);
    },
    get(name) {
        return this.definitions.get(name);
    }
};
globalThis.MutationObserver = class {
    observe() {}
};
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = { baseURI: "https://example.test/" };
globalThis.history = { replaceState() {}, pushState() {} };
globalThis.location = { replace() {}, hash: "" };

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { createPageClassFromJsDefinition } = await import("../loaders/page-js.js");
const { default: Navigation } = await import("../navigation.js");
const { default: Page } = await import("../page.js");
const { default: xshell } = await import("../xshell.js");

class ReactiveStateEngineFactory {
    constructor(state) {
        this.state = state;
    }

    create(handler) {
        const state = { ...this.state };
        return new Proxy(state, {
            set(target, property, value) {
                const oldValue = target[property];
                if (oldValue !== value) {
                    target[property] = value;
                    handler.stateChange(property, oldValue, value);
                    handler.invalidate(property);
                }
                return true;
            }
        });
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
            return resource.startsWith("state-engine:") ? ReactiveStateEngineFactory : RenderEngineFactory;
        }
    };
    xshell._bus = { emit() {} };
}

function createNavigation(mode, xpages) {
    const replaceCalls = [];
    const pushCalls = [];
    const locationReplacements = [];
    globalThis.history = {
        replaceState(state, title, url) {
            replaceCalls.push(url);
        },
        pushState(state, title, url) {
            pushCalls.push(url);
        }
    };
    globalThis.location = {
        hash: "",
        replace(url) {
            locationReplacements.push(url);
        }
    };
    const container = {
        querySelectorAll(selector) {
            return xpages;
        }
    };
    const navigation = new Navigation({
        areas: { resolvePath() { return null; } },
        bus: { emit() {} },
        config: {
            app: { basePath: "https://example.test/" },
            xshell: { navigation: { mode, hashPrefix: "#!" } }
        },
        container
    });
    return { navigation, replaceCalls, pushCalls, locationReplacements };
}

function createDefinition(controller = () => ({})) {
    return { meta: { name: "page-query-reflection-test" }, controller };
}

function createContext() {
    return { resourceDefinition: { moduleId: "test" } };
}

async function createPageClass(properties, controller = () => ({})) {
    configureDefinitionLoaders();
    return await createPageClassFromJsDefinition(
        "/page.js",
        createContext(),
        createDefinition(controller),
        { properties }
    );
}

function createXPage(src, layout = "main") {
    return {
        src,
        getAttribute(name) {
            return name === "layout" ? layout : null;
        },
        _synchronizePageSrc(value) {
            this.src = value;
        }
    };
}

test("reflected Page state patches only its own query and removes contract defaults", async () => {
    const host = createXPage("/page.js?page-index=2&enabled=false&ratio=1.5&external=x");
    const { navigation, replaceCalls, pushCalls } = createNavigation("path", [host]);
    navigation._stack = [navigation.parseUrl(host.src)];
    xshell._navigation = navigation;
    const PageClass = await createPageClass({
        pageIndex: { type: "integer", default: 0, state: true, query: true, reflect: true },
        enabled: { type: "boolean", default: false, state: true, query: true, reflect: true },
        ratio: { type: "number", default: 1, state: true, query: true, reflect: true }
    });
    const page = new PageClass({ src: host.src, context: {} });
    page._host = host;

    assert.equal(page._state.pageIndex, 2);
    assert.equal(page._state.enabled, false);
    assert.equal(page._state.ratio, 1.5);
    assert.equal(replaceCalls.length, 0);
    const originalPage = page;

    page._state.pageIndex = 3;

    assert.equal(page, originalPage);
    assert.equal(navigation._stack[0].params["page-index"], "3");
    assert.equal(navigation._stack[0].params.external, "x");
    assert.equal(new URLSearchParams(replaceCalls.at(-1).split("?")[1]).get("page-index"), "3");
    assert.match(page.src, /page-index=3/);
    assert.match(host.src, /page-index=3/);
    assert.equal(pushCalls.length, 0);

    page._state.enabled = true;
    page._state.ratio = 2.5;

    assert.equal(navigation._stack[0].params.enabled, "true");
    assert.equal(navigation._stack[0].params.ratio, "2.5");

    page._state.pageIndex = 0;

    assert.equal(Object.hasOwn(navigation._stack[0].params, "page-index"), false);
    assert.equal(new URLSearchParams(replaceCalls.at(-1).split("?")[1]).has("page-index"), false);
    assert.doesNotMatch(page.src, /page-index=/);
    assert.equal(replaceCalls.length, 4);
});

test("stacked Page query reflection preserves other stack items and navigation metadata", () => {
    const xpages = [
        createXPage("/customers?status=active"),
        createXPage("/customer?id=42&tab=summary"),
        createXPage("/order?id=100")
    ];
    const { navigation, replaceCalls, pushCalls } = createNavigation("path", xpages);
    navigation._stack = [
        navigation.parseUrl(xpages[0].src),
        { ...navigation.parseUrl(xpages[1].src), nav: { title: "Customer", marker: "keep" } },
        navigation.parseUrl(xpages[2].src)
    ];
    const page = new Page({ src: xpages[1].src, context: {} });
    page._host = xpages[1];
    xshell._navigation = navigation;

    page.replaceQuery({ tab: "details" });

    assert.deepEqual(navigation._stack[0].params, { status: "active" });
    assert.deepEqual(navigation._stack[2].params, { id: "100" });
    assert.deepEqual(navigation._stack[1].params, { id: "42", tab: "details" });
    assert.deepEqual(navigation._stack[1].nav, { title: "Customer", marker: "keep" });
    assert.equal(pushCalls.length, 0);
    assert.equal(replaceCalls.length, 1);
    const restored = navigation._browserUrlToStack(replaceCalls[0]);
    assert.deepEqual(restored[1].params, { id: "42", tab: "details" });
    assert.deepEqual(restored[1].nav, { title: "Customer", marker: "keep" });
    assert.match(page.src, /tab=details/);
});

test("hash-mode Page query reflection uses replacement serialization", () => {
    const host = createXPage("/page.js?enabled=false");
    const { navigation, locationReplacements } = createNavigation("hash", [host]);
    navigation._stack = [navigation.parseUrl(host.src)];
    const page = new Page({ src: host.src, context: {} });
    page._host = host;
    xshell._navigation = navigation;

    page.replaceQuery({ enabled: true });

    assert.equal(locationReplacements.length, 1);
    assert.equal(locationReplacements[0].startsWith("#!/"), true);
    assert.equal(navigation._browserUrlToStack(locationReplacements[0])[0].params.enabled, "true");
});

test("dialog and embedded Pages patch local src without changing the navigation stack", () => {
    const stackHost = createXPage("/page.js?root=1");
    const dialogHost = createXPage("/dialog.js?tab=summary&external=x", "dialog");
    const { navigation, replaceCalls, locationReplacements } = createNavigation("path", [stackHost]);
    navigation._stack = [navigation.parseUrl(stackHost.src)];
    const page = new Page({ src: dialogHost.src, context: {} });
    page._host = dialogHost;
    xshell._navigation = navigation;

    page.replaceQuery({ tab: "details" });

    assert.match(page.src, /tab=details/);
    assert.equal(navigation._stack[0].params.root, "1");
    assert.equal(replaceCalls.length, 0);
    assert.equal(locationReplacements.length, 0);
});

test("attribute reflection remains independent from Page-query metadata", async () => {
    configureDefinitionLoaders();
    const Component = await createComponentClassFromJsDefinition(
        "component.js",
        createContext(),
        { meta: { name: "query-reflection-component" }, controller: () => ({}) },
        { properties: { value: { type: "string", default: "default", state: true, attribute: true, query: true, reflect: true } } }
    );
    const component = new Component();

    component.value = "changed";

    assert.equal(component.getAttribute("value"), "changed");
});
