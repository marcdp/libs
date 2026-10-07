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
const { default: Areas } = await import("../areas.js");
const { default: Navigation } = await import("../navigation.js");
const { default: xshell } = await import("../xshell.js");
const { contract, default: anchorDefinition } = await import("../../x/components/x-anchor.js");

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

function createNavigation({ areas = null, mode = "path", basePath = "https://example.test/" } = {}) {
    return new Navigation({
        areas: areas || {
            resolveAreaId() { return null; },
            getArea() { return null; },
            getCurrentArea() { return null; },
            getDefaultArea() { return null; },
            resolveHref() { return null; },
            resolvePath() { return null; }
        },
        bus: {},
        config: {
            app: { basePath },
            xshell: { navigation: { mode, hashPrefix: "#!" } }
        },
        container: {}
    });
}

function createRouteAreas() {
    const config = {
        xshell: {
            assetsBasePath: "/_assets",
            areas: {
                default: "demo",
                global: [],
                definitions: { demo: { prefix: "/demo", modules: ["x-demo"] } }
            }
        }
    };
    const module = {
        id: "x-demo",
        routes: { "/repository/{repositoryId}/items": "/_assets/x-demo/pages/items.js" },
        config: { menus: {} }
    };
    const areas = new Areas({ config, bus: { addEventListener() {}, emit() {} } });
    areas.init({ modules: { getModuleById(id) { return id === module.id ? module : null; } } });
    return areas;
}

async function createAnchor(attributes = {}, navigation = createNavigation()) {
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
        meta: { ...anchorDefinition.meta, id: "x-anchor-test" }
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

test("x-anchor retains its canonical href while exposing and navigating the friendly URL", async () => {
    const area = { id: "demo", prefix: "/demo" };
    const areas = {
        resolveAreaId(src) { return src.startsWith("/demo/") ? "demo" : null; },
        getArea(id) { return id === "demo" ? area : null; },
        getCurrentArea() { return area; },
        getDefaultArea() { return area; },
        resolveHref(href, areaId) {
            return areaId === "demo" && href === "/demo/_assets/x-demo/pages/basic.js" ? { path: "/demo/navigation/basic" } : null;
        },
        resolvePath() { return null; }
    };
    const navigation = createNavigation({ areas });
    const { anchor, navigateCalls } = await createAnchor({}, navigation);
    anchor.parentNode.page.src = "/demo/_assets/x-demo/pages/origin.js";
    anchor.href = "/_assets/x-demo/pages/basic.js";
    anchor.query = { name: "lucas" };

    anchor.onCommand("stateChange", {});

    assert.equal(anchor.href, "/_assets/x-demo/pages/basic.js");
    assert.equal(anchor._state.href, "/_assets/x-demo/pages/basic.js");
    assert.equal(anchor._state.hrefReal, "/demo/navigation/basic?name=lucas");
    assert.match(anchorDefinition.template, /x-attr:href="state\.hrefReal"/);

    const event = {
        button: 0,
        defaultPrevented: false,
        metaKey: false,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        preventDefault() { this.defaultPrevented = true; }
    };
    anchor.onCommand("click", { event });

    assert.equal(navigateCalls.length, 1);
    assert.equal(navigateCalls[0].href, "/_assets/x-demo/pages/basic.js");
    assert.equal(event.defaultPrevented, true);
});

test("x-anchor delegates route-aware native href generation to Navigation while retaining its canonical target", async () => {
    const navigation = createNavigation({ areas: createRouteAreas(), basePath: "https://example.test/app/" });
    const { anchor, navigateCalls } = await createAnchor({}, navigation);
    anchor.parentNode.page.src = "/demo/_assets/x-demo/pages/origin.js";
    anchor.href = "/_assets/x-demo/pages/items.js";
    anchor.query = { repositoryId: "12", sort: "name" };

    anchor.onCommand("stateChange", {});

    assert.equal(anchor.href, "/_assets/x-demo/pages/items.js");
    assert.equal(anchor._state.href, "/_assets/x-demo/pages/items.js");
    assert.equal(anchor._state.hrefReal, "/app/demo/repository/12/items?sort=name");
    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/12/items?sort=name")),
        "/demo/_assets/x-demo/pages/items.js?repositoryId=12&sort=name"
    );

    const event = {
        button: 0,
        defaultPrevented: false,
        metaKey: false,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        preventDefault() { this.defaultPrevented = true; }
    };
    anchor.onCommand("click", { event });

    assert.equal(navigateCalls.length, 1);
    assert.equal(navigateCalls[0].href, "/_assets/x-demo/pages/items.js");
    assert.deepEqual(navigateCalls[0].params, { repositoryId: "12", sort: "name" });
    assert.equal(event.defaultPrevented, true);
});

test("x-anchor leaves target and modifier clicks to native browser behavior", async () => {
    const { anchor, navigateCalls } = await createAnchor();
    anchor.href = "/customers";
    const targetEvent = {
        button: 0,
        defaultPrevented: false,
        metaKey: false,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        preventDefault() { this.defaultPrevented = true; }
    };
    anchor.target = "_blank";
    anchor.onCommand("click", { event: targetEvent });

    const modifierEvent = { ...targetEvent, defaultPrevented: false, ctrlKey: true };
    anchor.target = null;
    anchor.onCommand("click", { event: modifierEvent });

    assert.equal(navigateCalls.length, 0);
    assert.equal(targetEvent.defaultPrevented, false);
    assert.equal(modifierEvent.defaultPrevented, false);
});

test("x-anchor leaves external links to native browser behavior", async () => {
    const { anchor, navigateCalls } = await createAnchor();
    anchor.href = "mailto:user@example.com";
    anchor.onCommand("stateChange", {});
    const event = {
        button: 0,
        defaultPrevented: false,
        metaKey: false,
        ctrlKey: false,
        shiftKey: false,
        altKey: false,
        preventDefault() { this.defaultPrevented = true; }
    };

    anchor.onCommand("click", { event });

    assert.equal(anchor._state.hrefReal, "mailto:user@example.com");
    assert.equal(navigateCalls.length, 0);
    assert.equal(event.defaultPrevented, false);
});
