import assert from "node:assert/strict";
import test from "node:test";

let initialAttributes = {};

class FakeElement {
    constructor() {
        this._attributes = new Map(Object.entries(initialAttributes));
        this.parentNode = null;
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

    getRootNode() {
        return null;
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = class {
    replaceSync() {}
};
globalThis.MutationObserver = class {
    observe() {}
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
globalThis.window = { customElements: globalThis.customElements };

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { default: Navigation } = await import("../navigation.js");
const { default: xshell } = await import("../xshell.js");
const { contract, default: anchorDefinition } = await import("../../modules/x/components/x-anchor.js");

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
    slots = [""];

    init() {}
}

function createNavigation() {
    return new Navigation({
        areas: {},
        bus: {},
        config: {
            app: { basePath: "https://example.test/" },
            xshell: { navigation: { mode: "path", hashPrefix: "#!" } }
        },
        container: {}
    });
}

async function createAnchor(attributes = {}) {
    const navigation = createNavigation();
    const navigateCalls = [];
    navigation.navigate = params => navigateCalls.push(params);
    xshell._config = {
        modules: {
            test: {
                defaults: {
                    component: { stateEngine: "test", renderEngine: "test" }
                }
            }
        }
    };
    xshell._loader = {
        async load(resource) {
            return resource.startsWith("state-engine:") ? StateEngineFactory : RenderEngineFactory;
        }
    };
    xshell._services = {
        resolve(name) {
            if (name === "navigation") return navigation;
            throw new Error(`Unexpected service: ${String(name)}`);
        }
    };
    const definition = {
        ...anchorDefinition,
        meta: { ...anchorDefinition.meta, name: "x-anchor-test" }
    };
    const Anchor = await createComponentClassFromJsDefinition(
        "x-anchor.js",
        { resourceDefinition: { moduleId: "test" } },
        definition,
        contract
    );
    initialAttributes = attributes;
    const anchor = new Anchor();
    initialAttributes = {};
    anchor.parentNode = {
        tagName: "X-PAGE",
        page: { breadcrumb: [], host: null, src: "/current" }
    };
    return { anchor, navigateCalls };
}

test("x-anchor exposes query instead of qs", () => {
    assert.deepEqual(contract.properties.query, { type: "object", default: {}, attribute: true, state: true, description: "" });
    assert.equal(Object.hasOwn(contract.properties, "qs"), false);
});

test("x-anchor maps query attributes into its generated href and click navigation", async () => {
    const { anchor, navigateCalls } = await createAnchor({ "query-name": "lucas", "query-count": "123" });
    anchor.href = "/customers";

    assert.deepEqual(anchor.query, { name: "lucas", count: "123" });
    assert.equal("qs" in anchor, false);

    anchor.onCommand("stateChange", {});
    assert.equal(anchor._state.hrefReal, "/customers?name=lucas&count=123");

    const event = {
        button: 0,
        defaultPrevented: false,
        metaKey: false,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        preventDefault() {
            this.defaultPrevented = true;
        }
    };
    anchor.onCommand("click", { event });

    assert.equal(navigateCalls.length, 1);
    assert.equal(navigateCalls[0].href, "/customers");
    assert.equal(navigateCalls[0].params, anchor.query);
    assert.deepEqual(navigateCalls[0].params, { name: "lucas", count: "123" });
    assert.equal(event.defaultPrevented, true);
});

test("x-anchor accepts its query object programmatically", async () => {
    const { anchor } = await createAnchor();
    anchor.href = "/customers";
    anchor.query = { name: "lucas", count: "123" };

    anchor.onCommand("stateChange", {});

    assert.equal(anchor._state.hrefReal, "/customers?name=lucas&count=123");
});
