import assert from "node:assert/strict";
import test from "node:test";

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

class FakeElement {
    constructor() {
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
        const element = new FakeNode();
        element.localName = name;
        element.setAttribute = () => {};
        element.removeAttribute = () => {};
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
    assert.equal(first.src, source);
    await page.unmount();
    assert.equal(host.childNodes.length, 0);
    assert.equal(page.host, null);
    await page.mount({ host });
    assert.equal(host.firstChild.src, source);
    assert.notEqual(host.firstChild, first);
    await page.unmount();
    await page.unload();
    await page.mount({ host });
    assert.equal(host.childNodes.length, 0);
});

test("Markdown adapter propagates component resolution/loading failure", async () => {
    xshell._loader = { async load() { throw new Error("component unavailable"); } };
    await assert.rejects(new LoaderPageMd().load("/guide.md", {}), /component unavailable/);
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
        meta: { name: "x-component-lifecycle-test" },
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
        meta: { name: "page-lifecycle-test" },
        state: { value: 1 },
        script() {
            return {
                load() { commands.push("load"); },
                mount() { commands.push("mount"); },
                unmount() { commands.push("unmount"); },
                unload() { commands.push("unload"); }
            };
        }
    }, {});
    const page = new PageClass({ src: "/pages/lifecycle.js", context: {} });
    const script = page._script;
    const disposable = { disposeCount: 0, dispose() { this.disposeCount += 1; } };
    page._disposables.push(disposable);
    const host = { nodeName: "X-PAGE", getAttribute() { return "/pages/lifecycle.js"; } };

    await page.load();
    await page.load();
    await page.mount({ host });
    const firstRenderEngine = page._renderEngine;
    page._state.value = 2;
    await page.unmount();

    assert.deepEqual(commands, ["load", "mount", "unmount"]);
    assert.equal(page._script, script);
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
    assert.deepEqual(commands, ["old unmount", "old unload", "new load", "new mount"]);
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
