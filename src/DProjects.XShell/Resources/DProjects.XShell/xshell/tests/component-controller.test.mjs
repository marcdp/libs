import assert from "node:assert/strict";
import test from "node:test";

class FakeEventTarget {
    listeners = new Map();
    addEventListener(name, listener) {
        this.listeners.set(name, [...(this.listeners.get(name) ?? []), listener]);
    }
    removeEventListener(name, listener) {
        this.listeners.set(name, (this.listeners.get(name) ?? []).filter(item => item !== listener));
    }
    dispatchEvent(event) {
        for (const listener of this.listeners.get(event.type) ?? []) listener.call(this, event);
    }
}
class FakeShadowRoot extends FakeEventTarget {
    adoptedStyleSheets = [];
    querySelector() { return null; }
    querySelectorAll() { return []; }
}
class FakeElement extends FakeEventTarget {
    attributes = [];
    isConnected = true;
    attachShadow() {
        this.shadowRoot = new FakeShadowRoot();
        this.shadowRoot.host = this;
        return this.shadowRoot;
    }
    getAttribute(name) { return this.attributes.find(attribute => attribute.name === name)?.value ?? null; }
    setAttribute(name, value) {
        const attribute = this.attributes.find(item => item.name === name);
        if (attribute) attribute.value = String(value);
        else this.attributes.push({ name, value: String(value) });
    }
    removeAttribute(name) { this.attributes = this.attributes.filter(attribute => attribute.name !== name); }
    getRootNode() { return this; }
}

const registry = new Map();
globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.MutationObserver = class { observe() {} disconnect() {} };
globalThis.CustomEvent = class { constructor(type, options = {}) { this.type = type; Object.assign(this, options); } };
globalThis.customElements = { get(name) { return registry.get(name); }, define(name, type) { registry.set(name, type); } };
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = {
    baseURI: "http://localhost/",
    body: new FakeElement(),
    adoptedStyleSheets: [],
    createElement() { return new FakeElement(); }
};
globalThis.requestAnimationFrame = callback => { callback(); return 1; };

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { default: xshell } = await import("../xshell.js");

class StateEngineFactory {
    constructor(state) { this.state = state; }
    create() { return { ...this.state }; }
}
let templateHandler;
class RenderEngineFactory {
    constructor(template, context, templateRenderer) {
        this.dependencies = [];
        this.slots = templateRenderer?.slots ?? [];
    }
    init() {}
    create(options) {
        templateHandler = options.handler;
        return { mount() {}, unmount() {}, render() {} };
    }
}
xshell._config = { modules: { test: { defaults: { component: { stateEngine: "test", renderEngine: "test" } } } } };
xshell._loader = { async load(resource) { return resource.startsWith("state-engine:") ? StateEngineFactory : RenderEngineFactory; } };
xshell._services = { resolve() { return null; } };
xshell._modules = { getModuleById() { return {}; } };

const context = { resourceDefinition: { moduleId: "test" } };
const contract = (methods = {}, slots = {}) => ({ description: "Controller test.", properties: {}, events: {}, slots, methods });
const definition = (id, overrides = {}) => ({ meta: { id }, state: {}, style: "", template: "", controller() { return {}; }, ...overrides });

test("controller methods keep their context through lifecycle, events, timers and public calls", async () => {
    const calls = [];
    const target = new FakeEventTarget();
    let controller;
    let injectedHost;
    const Component = await createComponentClassFromJsDefinition("controller.js", context, definition("x-controller-runtime-test", {
        controller({ host, events, timer }) {
            injectedHost = host;
            controller = {
                load() { calls.push(["load", this]); events.on(target, "run", "eventsHandler"); timer.setTimeout(0, "timerHandler"); },
                mount() { calls.push(["mount", this]); this.refresh(); },
                unmount() { calls.push(["unmount", this]); },
                unload() { calls.push(["unload", this]); },
                stateChange() { calls.push(["stateChange", this]); },
                refresh() { calls.push(["refresh", this]); },
                templateHandler() { calls.push(["template", this]); },
                eventsHandler() { calls.push(["events", this]); },
                timerHandler() { calls.push(["timer", this]); },
                publicMethod(...args) { calls.push(["public", this]); return args; }
            };
            return controller;
        }
    }), contract({ publicMethod: { description: "Public test method." } }));
    const element = new Component();
    assert.equal(injectedHost, element);
    element.connectedCallback();
    templateHandler("templateHandler", { event: { type: "click" } });
    assert.deepEqual(element.publicMethod({ value: 1 }, "two"), [{ value: 1 }, "two"]);
    target.dispatchEvent({ type: "run" });
    await new Promise(resolve => setTimeout(resolve, 10));
    element.disconnectedCallback();
    element.connectedCallback();
    element.disconnectedCallback();
    await element.unload();

    assert.deepEqual(Object.fromEntries(["load", "mount", "unmount", "unload", "stateChange", "template", "refresh", "events", "timer", "public"]
        .map(name => [name, calls.filter(([called]) => called === name).length])),
        { load: 1, mount: 2, unmount: 2, unload: 1, stateChange: 2, template: 1, refresh: 2, events: 1, timer: 1, public: 1 });
    assert.ok(calls.every(([, receiver]) => receiver === controller));
    assert.equal(element.templateHandler, undefined);
    assert.equal(element.refresh, undefined);
    assert.equal(Object.hasOwn(element, "onCommand"), false);
});

test("contract methods cannot replace runtime methods", async () => {
    await assert.rejects(createComponentClassFromJsDefinition("collision.js", context, definition("x-controller-collision-test", {
        controller() { return { unload() {} }; }
    }), contract({ unload: { description: "Collision." } })), /cannot expose public method 'unload'/);
});

test("slot validation uses render-engine metadata, ignores raw markup and accepts contract supersets", async () => {
    await createComponentClassFromJsDefinition("raw.js", context, definition("x-raw-slot-test", {
        template: '<slot name="ghost"></slot>', templateRenderer: { slots: [] }
    }), contract());
    await createComponentClassFromJsDefinition("superset.js", context, definition("x-slot-superset-test", {
        templateRenderer: { slots: ["footer"] }
    }), contract({}, { footer: { description: "Footer." }, tools: { description: "Tools." } }));
    await assert.rejects(createComponentClassFromJsDefinition("named.js", context, definition("x-missing-named-slot-test", {
        templateRenderer: { slots: ["footer"] }
    }), contract()), /slot 'footer'/);
    await assert.rejects(createComponentClassFromJsDefinition("default.js", context, definition("x-missing-default-slot-test", {
        templateRenderer: { slots: [""] }
    }), contract()), /slot '\(default\)'/);
});
