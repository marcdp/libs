import assert from "node:assert/strict";
import test from "node:test";
import ComponentContractSchema from "../schemas/component.contract.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js";
import Dialog from "../dialog.js";
import { contract as pickerContract } from "../../x/pages/dialog-picker.js";

const animationFrames = [];

class FakeNode {
    adoptedStyleSheets = [];
    childNodes = [];
    nodeName = "X-PAGE";

    get firstChild() {
        return this.childNodes[0] ?? null;
    }

    appendChild(node) {
        this.childNodes.push(node);
        node.parentNode = this;
        return node;
    }

    removeChild(node) {
        this.childNodes.splice(this.childNodes.indexOf(node), 1);
        node.parentNode = null;
        node.disconnectedCallback?.();
        return node;
    }

    replaceChildren(...nodes) {
        this.childNodes = nodes;
    }

    querySelector() {
        return null;
    }
}

class FakeElement extends FakeNode {
    constructor() {
        super();
        this._attributes = new Map();
        this._listeners = new Map();
    }

    attachShadow() {
        this.shadowRoot = new FakeNode();
        return this.shadowRoot;
    }

    addEventListener(name, listener) {
        const listeners = this._listeners.get(name) ?? [];
        listeners.push(listener);
        this._listeners.set(name, listeners);
    }

    dispatchEvent(event) {
        for (const listener of this._listeners.get(event.type) ?? []) listener(event);
    }

    contains() {
        return false;
    }

    getAttribute(name) {
        return this._attributes.get(name) ?? null;
    }

    hasAttribute(name) {
        return this._attributes.has(name);
    }

    setAttribute(name, value) {
        this._attributes.set(name, String(value));
    }

    removeAttribute(name) {
        this._attributes.delete(name);
    }

    get attributes() {
        return [...this._attributes.entries()].map(([name, value]) => ({ name, value }));
    }

    remove() {
        this.removed = true;
        this.parentNode?.removeChild(this);
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
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = {
    adoptedStyleSheets: [],
    baseURI: "https://example.test/",
    body: { querySelectorAll() { return []; } },
    createElement(name) {
        if (name === "x-page") return new (customElements.get(name))();
        const element = new FakeElement();
        element.localName = name;
        return element;
    }
};
globalThis.requestAnimationFrame = callback => {
    animationFrames.push(callback);
    return animationFrames.length;
};

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { createPageClassFromJsDefinition } = await import("../loaders/page-js.js");
const { default: XPage } = await import("../x-page.js");
const { default: Navigation } = await import("../navigation.js");
const { default: xshell } = await import("../xshell.js");
const { default: Page } = await import("../page.js");
const { default: LoaderPageMd } = await import("../loaders/page-md.js");

test("Markdown adapter loads its component without fetching, and follows the Page lifecycle across mounts", async (t) => {
    const resources = [];
    t.mock.method(globalThis, "fetch", () => { throw new Error("page-md must not fetch Markdown"); });
    xshell._config = { xshell: { ui: { component: { markdown: "x-markdown" } } } };
    xshell._loader = { async load(resource) { resources.push(resource); } };
    xshell._bus = { emit() {} };
    const source = "/_assets/xshell-docs/pages/10-architecture/100-services.md";
    const MarkdownPage = await new LoaderPageMd().load(source, {});
    assert.deepEqual(resources, ["component:x-markdown"]);
    const page = new MarkdownPage({ src: source + "?view=one#section", context: {} });
    assert.ok(page instanceof Page);
    assert.equal(page.src, source + "?view=one#section");
    assert.equal(page.label, null); // menu/navigation metadata can supply the label
    await page.load();
    assert.equal(page._loaded, true);
    const host = new FakeNode();
    await page.mount({ host });
    const first = host.firstChild;
    assert.equal(first.localName, "x-markdown");
    assert.equal(first.getAttribute("src"), source);
    assert.equal(first.getAttribute("src").startsWith("string:"), false);
    await page.unmount();
    assert.equal(host.childNodes.length, 0);
    assert.equal(page.host, null);
    await page.mount({ host });
    assert.equal(host.firstChild.getAttribute("src"), source);
    assert.notEqual(host.firstChild, first);
    await page.unmount();
    await page.unload();
    await page.mount({ host });
    assert.equal(host.childNodes.length, 0);
    assert.equal(globalThis.fetch.mock.callCount(), 0);
});

test("Markdown adapter propagates component resolution/loading failure", async () => {
    xshell._config = { xshell: { ui: { component: { markdown: "x-markdown" } } } };
    xshell._loader = { async load() { throw new Error("component unavailable"); } };
    await assert.rejects(new LoaderPageMd().load("/guide.md", {}), /component unavailable/);
});

test("x-page intercepts only application anchors and preserves native clicks", async (t) => {
    const cases = [
        { href: "/customers", intercept: true },
        { href: "/_assets/xshell-docs/pages/10-architecture/100-services.md", intercept: true },
        { href: "relative-page.js", intercept: true },
        { href: "another-document.md?view=one#section", intercept: true },
        { href: "#!/customers?view=one", intercept: true },
        { href: "/customers", target: "_self", intercept: true },
        { href: "/customers", target: "_SELF", intercept: true },
        { href: "#section" },
        { href: "#" },
        { href: "https://example.com/page" },
        { href: "http://example.com/page" },
        { href: "mailto:user@example.com" },
        { href: "tel:+1234" },
        { href: "data:text/plain,hello" },
        { href: "blob:https://example.com/id" },
        { href: "Custom+v1.2-test:resource" },
        { href: "//example.com/page" },
        { href: "/customers", event: { ctrlKey: true } },
        { href: "/customers", event: { metaKey: true } },
        { href: "/customers", event: { shiftKey: true } },
        { href: "/customers", event: { altKey: true } },
        { href: "/customers", event: { button: 1 } },
        { href: "/customers", event: { button: 2 } },
        { href: "/customers", event: { defaultPrevented: true } },
        { href: "/customers", download: true },
        { href: "/customers", target: "_blank" },
        { href: "/customers", target: "_parent" },
        { href: "/customers", target: "_top" },
        { href: "/customers", target: "preview" },
        { href: "" },
        { href: null }
    ];
    for (const mode of ["path", "hash"]) {
        for (const scenario of cases) {
            await t.test(`${mode}: ${JSON.stringify(scenario)}`, () => {
                // exercise the registered click listener with real Navigation URL parsing
                const navigation = new Navigation({
                    areas: {}, bus: {}, container: {},
                    config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode, hashPrefix: "#!" } } }
                });
                const calls = [];
                navigation.navigate = item => calls.push(item);
                xshell._navigation = navigation;
                const xpage = new XPage();
                xpage._page = new Page({ src: "/pages/current.js", context: {} });
                const anchor = document.createElement("a");
                if (scenario.href !== null) anchor.setAttribute("href", scenario.href);
                if (scenario.download) anchor.setAttribute("download", "");
                anchor.target = scenario.target || "";
                anchor.closest = selector => selector === "a" ? anchor : xpage;
                xpage.contains = element => element === anchor;
                let prevented = 0;
                let stopped = 0;
                const event = {
                    type: "click", target: anchor, button: 0, defaultPrevented: false,
                    metaKey: false, ctrlKey: false, shiftKey: false, altKey: false,
                    ...scenario.event,
                    preventDefault() { prevented++; this.defaultPrevented = true; },
                    stopPropagation() { stopped++; }
                };
                xpage.dispatchEvent(event);
                const expected = scenario.intercept ? 1 : 0;
                assert.equal(calls.length, expected);
                assert.equal(prevented, expected);
                assert.equal(stopped, expected);
                if (scenario.intercept) {
                    assert.deepEqual(calls[0], { ...navigation.parseUrl(scenario.href), open: "auto", page: xpage.page });
                    assert.equal(calls[0].page, xpage.page);
                }
            });
        }
    }
});

const renderEngines = [];

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

    create() {
        const renderEngine = {
            mountCount: 0,
            renderCount: 0,
            unmountCount: 0,
            mount() {
                this.mountCount += 1;
            },
            render() {
                this.renderCount += 1;
            },
            unmount() {
                this.unmountCount += 1;
            }
        };
        renderEngines.push(renderEngine);
        return renderEngine;
    }
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

function flushAnimationFrames() {
    const callbacks = animationFrames.splice(0);
    for (const callback of callbacks) {
        callback();
    }
}

test("component preserves its instance lifetime across reconnects and unloads once", async () => {
    configureDefinitionLoaders();
    renderEngines.length = 0;
    animationFrames.length = 0;
    const commands = [];
    const Component = await createComponentClassFromJsDefinition("component-lifecycle.js", createContext(), {
        meta: { id: "x-component-lifecycle-test" },
        state: { value: 1 },
        controller() {
            return {
                load() { commands.push("load"); },
                mount() { commands.push("mount"); },
                unmount() { commands.push("unmount"); },
                unload() { commands.push("unload"); }
            };
        }
    }, {});
    const component = new Component();
    const controller = component._controller;
    const disposable = { disposeCount: 0, dispose() { this.disposeCount += 1; } };
    component._disposables.push(disposable);

    component.connectedCallback();
    const firstRenderEngine = component._renderEngine;
    component._state.value = 2;
    component.disconnectedCallback();

    assert.deepEqual(commands, ["load", "mount", "unmount"]);
    assert.equal(component._controller, controller);
    assert.equal(component._state.value, 2);
    assert.equal(component._renderEngine, null);
    assert.equal(firstRenderEngine.unmountCount, 1);
    assert.equal(disposable.disposeCount, 0);
    assert.equal(component._disposables.length, 1);

    component.connectedCallback();
    const secondRenderEngine = component._renderEngine;
    assert.notEqual(secondRenderEngine, firstRenderEngine);
    assert.deepEqual(commands, ["load", "mount", "unmount", "mount"]);

    component.invalidate();
    component.disconnectedCallback();
    assert.doesNotThrow(flushAnimationFrames);
    assert.equal(secondRenderEngine.renderCount, 0);

    await component.unload();
    await component.unload();
    assert.deepEqual(commands, ["load", "mount", "unmount", "mount", "unmount", "unload"]);
    assert.equal(disposable.disposeCount, 1);
    assert.deepEqual(component._disposables, []);
});

test("page preserves state and disposables until its final unload", async () => {
    configureDefinitionLoaders();
    renderEngines.length = 0;
    animationFrames.length = 0;
    const commands = [];
    const PageClass = await createPageClassFromJsDefinition("page-lifecycle.js", createContext(), {
        meta: { id: "page-lifecycle-test" },
        state: { value: 1 },
        controller() {
            return {
                load() { commands.push("load"); },
                mount() { commands.push("mount"); },
                unmount() { commands.push("unmount"); },
                unload() { commands.push("unload"); }
            };
        }
    }, {});
    const page = new PageClass({ src: "/pages/lifecycle.js", context: {} });
    const controller = page._controller;
    const disposable = { disposeCount: 0, dispose() { this.disposeCount += 1; } };
    page._disposables.push(disposable);
    const root = { adoptedStyleSheets: [] };
    const host = { nodeName: "X-PAGE", getAttribute() { return "/pages/lifecycle.js"; }, getRootNode() { return root; } };

    await page.load();
    await page.load();
    await page.mount({ host });
    const firstRenderEngine = page._renderEngine;
    page._state.value = 2;
    await page.unmount();

    assert.deepEqual(commands, ["load", "mount", "unmount"]);
    assert.equal(page._controller, controller);
    assert.equal(page._state.value, 2);
    assert.equal(page._renderEngine, null);
    assert.equal(disposable.disposeCount, 0);
    assert.equal(page._disposables.length, 1);

    await page.mount({ host });
    const secondRenderEngine = page._renderEngine;
    assert.notEqual(secondRenderEngine, firstRenderEngine);
    await page.unmount();
    await page.unmount();
    assert.deepEqual(commands, ["load", "mount", "unmount", "mount", "unmount", "unmount"]);

    page.invalidate();
    assert.doesNotThrow(flushAnimationFrames);
    assert.equal(secondRenderEngine.renderCount, 0);

    await page.unload();
    await page.unload();
    assert.deepEqual(commands, ["load", "mount", "unmount", "mount", "unmount", "unmount", "unload"]);
    assert.equal(disposable.disposeCount, 1);
    assert.deepEqual(page._disposables, []);
});

test("x-page unmounts then unloads a replaced page and only unmounts on disconnection", async () => {
    const commands = [];
    const oldPage = {
        src: "/pages/new.js",
        async mount() { commands.push("old mount"); },
        async unmount() { commands.push("old unmount"); },
        async unload() { commands.push("old unload"); }
    };
    class NewPage {
        label = "";
        icon = "";

        constructor() {}

        async load() { commands.push("new load"); }

        async mount() { commands.push("new mount"); }
    }
    xshell._debug = { log() {} };
    xshell._config = { xshell: { ui: { layout: { embed: "x-layout-test" } } } };
    xshell._areas = {
        resolveAreaId() { return ""; },
        getArea() { return null; },
        getMenuitemBreadcrumb() { return []; }
    };
    xshell._modules = { resolveModuleId() { return "test"; } };
    xshell._loader = {
        async load(resource) {
            return resource.startsWith("page:") ? NewPage : class {};
        }
    };
    const xpage = new XPage();
    xpage._src = "/pages/new.js";
    xpage._page = oldPage;

    xpage.disconnectedCallback();
    await Promise.resolve();
    assert.deepEqual(commands, ["old unmount"]);

    commands.length = 0;
    xpage.connectedCallback();
    await Promise.resolve();
    assert.deepEqual(commands, ["old mount"]);

    commands.length = 0;
    await xpage.load();
    assert.deepEqual(commands, ["new load", "old unmount", "old unload", "new mount"]);
});

test("x-page discards an older load without disturbing the newer active Page", async () => {
    const commands = [];
    let finishALoad;
    let markALoadStarted;
    const aLoadStarted = new Promise(resolve => { markALoadStarted = resolve; });
    const aLoadGate = new Promise(resolve => { finishALoad = resolve; });
    class PageA {
        label = "";
        icon = "";
        async load() { commands.push("A load"); markALoadStarted(); await aLoadGate; }
        async mount() { commands.push("A mount"); }
        async unmount() { commands.push("A unmount"); }
        async unload() { commands.push("A unload"); }
    }
    class PageB {
        label = "";
        icon = "";
        async load() { commands.push("B load"); }
        async mount() { commands.push("B mount"); }
        async unmount() { commands.push("B unmount"); }
        async unload() { commands.push("B unload"); }
    }
    xshell._debug = { log() {} };
    xshell._config = { xshell: { ui: { layout: { embed: "x-layout-test" } } } };
    xshell._areas = { resolveAreaId() { return ""; }, getArea() { return null; }, getMenuitemBreadcrumb() { return []; } };
    xshell._modules = { resolveModuleId() { return "test"; } };
    xshell._loader = { async load(resource) { return resource === "page:/pages/a.js" ? PageA : PageB; } };
    const xpage = new XPage();
    xpage._src = "/pages/a.js";
    const loadA = xpage.load();
    await aLoadStarted;

    xpage._src = "/pages/b.js";
    await xpage.load();
    const activePage = xpage.page;
    assert.ok(activePage instanceof PageB);
    assert.deepEqual(commands, ["A load", "B load", "B mount"]);

    finishALoad();
    await loadA;
    assert.equal(xpage.page, activePage);
    assert.deepEqual(commands, ["A load", "B load", "B mount", "A unload"]);
});

test("dialog close completes after one final Page unload and host removal", async () => {
    const commands = [];
    let finishUnload;
    const unloadGate = new Promise(resolve => { finishUnload = resolve; });
    const container = new FakeNode();
    const navigation = new Navigation({
        areas: {},
        bus: {},
        config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode: "path", hashPrefix: "#!" } } },
        container
    });
    navigation._buildUrlFinal = item => item.href;
    const resultPromise = navigation._showDialog({ href: "/pages/dialog.js", context: {} });
    const xpage = container.firstChild;
    xpage._page = {
        async unmount() { commands.push("unmount"); },
        async unload() {
            commands.push("unload started");
            await unloadGate;
            commands.push("unload finished");
        }
    };
    let completed = false;
    resultPromise.then(() => { completed = true; });

    await xpage.close({ accepted: true });
    await Promise.resolve();
    assert.deepEqual(commands, ["unmount", "unload started"]);
    assert.equal(completed, false);
    assert.equal(container.firstChild, xpage);

    await xpage.close({ accepted: false });
    finishUnload();
    assert.deepEqual(await resultPromise, { accepted: true });
    assert.deepEqual(commands, ["unmount", "unload started", "unload finished"]);
    assert.equal(container.firstChild, null);
    assert.equal(xpage.page, null);
});

test("dialog close rejects its promise when final Page cleanup fails", async () => {
    const container = new FakeNode();
    const navigation = new Navigation({
        areas: {},
        bus: {},
        config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode: "path", hashPrefix: "#!" } } },
        container
    });
    navigation._buildUrlFinal = item => item.href;
    const resultPromise = navigation._showDialog({ href: "/pages/dialog.js", context: {} });
    const xpage = container.firstChild;
    const failure = new Error("unload failed");
    xpage._page = {
        async unmount() {},
        async unload() { throw failure; }
    };

    await xpage.close("done");
    await assert.rejects(resultPromise, error => error === failure);
});

test("navigation stack changes close dialogs through final Page cleanup", async () => {
    const commands = [];
    const container = new FakeNode();
    container.querySelectorAll = () => container.childNodes;
    const navigation = new Navigation({
        areas: {},
        bus: {},
        config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode: "path", hashPrefix: "#!" } } },
        container
    });
    navigation._buildUrlFinal = item => item.href;
    navigation.getXPages = () => [];
    const resultPromise = navigation._showDialog({ href: "/pages/dialog.js", context: {} });
    const xpage = container.firstChild;
    xpage._page = {
        async unmount() { commands.push("unmount"); },
        async unload() { commands.push("unload"); }
    };

    navigation._stackToDom();
    assert.equal(await resultPromise, null);
    assert.deepEqual(commands, ["unmount", "unload"]);
    assert.equal(container.firstChild, null);
});

test("navigation shrinks three stack Pages to one after asynchronous final unloads", async () => {
    const container = new FakeNode();
    container.querySelectorAll = () => container.childNodes;
    const navigation = new Navigation({
        areas: {}, bus: { emit() {} },
        config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode: "path", hashPrefix: "#!" } } },
        container
    });
    const counts = [0, 0, 0];
    const gates = [];
    for (let i = 0; i < 3; i++) {
        const host = new XPage();
        host._src = `/pages/p${i}.js`;
        host._page = {
            async unmount() {},
            async unload() {
                counts[i]++;
                if (i > 0) await new Promise(resolve => gates.push(resolve));
            }
        };
        container.appendChild(host);
    }
    const [root, middle, top] = container.childNodes;
    navigation._stack = [navigation.parseUrl(root.src)];
    const reconciliation = navigation._stackToDom();
    await new Promise(resolve => setImmediate(resolve));
    assert.deepEqual(counts, [0, 1, 0]);
    gates.shift()();
    await new Promise(resolve => setImmediate(resolve));
    assert.deepEqual(counts, [0, 1, 1]);
    gates.shift()();
    await reconciliation;
    assert.deepEqual(counts, [0, 1, 1]);
    assert.deepEqual(container.childNodes, [root]);
    assert.equal(middle.removed, true);
    assert.equal(top.removed, true);
    assert.equal(root.removed, undefined);
});

test("x-page loads URLs containing error through the normal Page loader", async () => {
    const loaded = [];
    xshell._config = { xshell: { ui: { layout: { embed: "x-layout-test" } } } };
    xshell._areas = { resolveAreaId() { return ""; }, getArea() { return null; }, getMenuitemBreadcrumb() { return []; } };
    xshell._modules = { resolveModuleId() { return "test"; } };
    xshell._loader = { async load(resource) {
        if (resource.startsWith("page:")) {
            loaded.push(resource);
            return class { async load() {} async mount() {} };
        }
        return class {};
    } };
    for (const src of ["/pages/error-report.js", "/pages/errors/index.js", "/pages/orders.js?status=error"]) {
        const host = new XPage();
        host._src = src;
        await host.load();
    }
    assert.deepEqual(loaded, ["page:/pages/error-report.js", "page:/pages/errors/index.js", "page:/pages/orders.js?status=error"]);
});

test("x-page and dialog preserve falsy results", async () => {
    const container = new FakeNode();
    const navigation = new Navigation({
        areas: {}, bus: {},
        config: { app: { basePath: "https://example.test/" }, xshell: { navigation: { mode: "path", hashPrefix: "#!" } } },
        container
    });
    navigation._buildUrlFinal = item => item.href;
    for (const value of [false, 0, "", null]) {
        const resultPromise = navigation._showDialog({ href: "/pages/dialog.js", context: {} });
        const host = container.firstChild;
        assert.equal(host.result, null);
        host._page = { async unmount() {}, async unload() {} };
        await host.close(value);
        assert.equal(await resultPromise, value);
    }
});

test("Component contract accepts boolean attribute observation and rejects string aliases", () => {
    const validator = new Validator(ComponentContractSchema, "2020-12");
    for (const attribute of [true, false]) {
        assert.equal(validator.validate({ properties: { item: { type: "string", attribute } } }).valid, true);
    }
    assert.equal(validator.validate({ properties: { item: { type: "string", attribute: "item-name" } } }).valid, false);
});

test("Component public-method diagnostic uses its canonical name", async () => {
    configureDefinitionLoaders();
    const Component = await createComponentClassFromJsDefinition("missing-method.js", createContext(), {
        meta: { id: "x-missing-method" }, controller() { return {}; }
    }, { methods: { submit: { description: "Submit" } } });
    assert.throws(() => new Component(), /Component 'x-missing-method'.*public method 'submit'/);
});

test("dialog.language forwards value to a radios picker", async () => {
    const dialog = new Dialog({ config: {}, navigation: {}, i18n: { config: { langs: [
        { id: "en", label: "English" }, { id: "es", label: "Spanish" }
    ] } } });
    let options;
    dialog.picker = async value => { options = value; return "es"; };
    assert.equal(await dialog.language({ value: "es", current: ["en"] }), "es");
    assert.deepEqual(options, { title: "Select language", message: "Language", value: "es", inputType: "radios",
        domain: [{ value: "es", label: "Spanish (es)" }], required: true });
});

test("picker Page contract includes radios", () => {
    assert.deepEqual(pickerContract.properties.inputType.enum, ["select", "radios"]);
});
