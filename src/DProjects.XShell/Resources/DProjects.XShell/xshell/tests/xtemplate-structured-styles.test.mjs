import assert from "node:assert/strict";
import test from "node:test";

class FakeStyle {
    values = {};
    setCalls = [];
    removeCalls = [];
    setProperty(name, value, priority) { this.setCalls.push(name); this.values[name] = { value, priority }; }
    removeProperty(name) { this.removeCalls.push(name); delete this.values[name]; }
}
class FakeFragment {
    childNodes = [];
    appendChild(child) {
        if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes);
        else this.childNodes.push(child);
        return child;
    }
    append(child) { this.appendChild(child); }
    querySelectorAll() { return []; }
}
class FakeElement {
    constructor(tag) {
        this.tagName = tag.toUpperCase();
        this.localName = tag.toLowerCase();
        this.childNodes = [];
        this.attributes = {};
        this.style = new FakeStyle();
        this.innerHTML = "";
    }
    appendChild(child) {
        if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes);
        else this.childNodes.push(child);
        return child;
    }
    append(child) { this.appendChild(child); }
    replaceChildren() { this.childNodes = []; }
    setAttribute(name, value) { this.attributes[name] = value; }
    removeAttribute(name) { delete this.attributes[name]; }
    querySelectorAll() { return []; }
}
globalThis.DocumentFragment = FakeFragment;
globalThis.HTMLElement = class {};
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.customElements = { get() {}, define() {} };
globalThis.window = { customElements: globalThis.customElements, location: { origin: "https://example.test" } };
globalThis.document = {
    createElement(tag) {
        const element = new FakeElement(tag);
        if (tag.toLowerCase() === "template") element.content = new FakeFragment();
        return element;
    },
    createDocumentFragment() { return new FakeFragment(); },
    createComment() { return new FakeElement("#comment"); },
    createTextNode() { return new FakeElement("#text"); }
};

const { default: createRenderEngineFactoryX } = await import("../render-engines/x.js");

test("render engine reconciles structured styles without disturbing external styles", () => {
    const renderer = (state, handler, invalidate, utils) => [utils.createVDOM("div", null, null, state.styles, null, { index: 0 })];
    const factory = createRenderEngineFactoryX("<div></div>", {}, { render: renderer, dependencies: [], slots: [] });
    factory.init();
    const host = new FakeElement("host");
    const state = { styles: {
        display: { value: "none", priority: "" }, width: { value: "100%", priority: "" },
        "margin-top": { value: "8px", priority: "" }, color: { value: "red", priority: "" }
    } };
    const engine = factory.create({ host, state, handler() {}, invalidate() {} });
    engine.render();
    const element = host.childNodes[0];
    assert.deepEqual(element.style.values, state.styles);
    assert.equal(Object.hasOwn(element.attributes, "style"), false);

    element.style.setProperty("external", "kept", "");
    element.style.setCalls = [];
    element.style.removeCalls = [];
    state.styles = {
        display: { value: "block", priority: "important" }, width: { value: "50%", priority: "" },
        color: { value: "red", priority: "" }
    };
    engine.render();
    assert.deepEqual(element.style.values, { ...state.styles, external: { value: "kept", priority: "" } });
    assert.deepEqual(element.style.setCalls, ["display", "width"]);
    assert.deepEqual(element.style.removeCalls, ["margin-top"]);
    assert.equal(Object.hasOwn(element.attributes, "style"), false);
});

test("compiler-literal structured style URLs are resolved before CSSOM application", () => {
    const renderer = (state, handler, invalidate, utils) => [utils.createVDOM("div", null, null, {
        "background-image": { value: utils.rewriteStyleValue("url(/pages/image.png)"), priority: "" }
    }, null, { index: 0 })];
    const context = {
        appBasePath: "/app",
        resourceDefinition: { modulePath: "/_assets/demo" },
        resourcePath: "/_assets/demo/pages/page.html"
    };
    const factory = createRenderEngineFactoryX("<div></div>", context, { render: renderer, dependencies: [], slots: [] });
    factory.init();
    const host = new FakeElement("host");
    factory.create({ host, state: {}, handler() {}, invalidate() {} }).render();
    assert.equal(host.childNodes[0].style.values["background-image"].value,
        "url(https://example.test/app/_assets/demo/pages/image.png)");
});

test("literal structured styles reject configuration schemes while dynamic values stay untouched", () => {
    const context = {
        appBasePath: "/app", resourceDefinition: { modulePath: "/_assets/demo" },
        resourcePath: "/_assets/demo/pages/page.html"
    };
    for (const scheme of ["app:", "APP:", "url:", "URL:"]) {
        const renderer = (state, handler, invalidate, utils) => [utils.createVDOM("div", null, null, {
            "background-image": { value: utils.rewriteStyleValue(`url(${scheme}/image.png)`), priority: "" }
        }, null, { index: 0 })];
        const factory = createRenderEngineFactoryX("", context, { render: renderer, dependencies: [], slots: [] });
        factory.init();
        assert.throws(() => factory.create({ host: new FakeElement("host"), state: {}, handler() {}, invalidate() {} }).render(), /scheme is not supported/);
    }
    const dynamicValue = "url(app:/runtime-value.png)";
    const renderer = state => [{ tag: "div", attrs: {}, props: {}, styles: { "background-image": { value: state.value, priority: "" } }, events: {}, options: { index: 0 }, children: [] }];
    const factory = createRenderEngineFactoryX("", context, { render: renderer, dependencies: [], slots: [] });
    factory.init();
    const host = new FakeElement("host");
    factory.create({ host, state: { value: dynamicValue }, handler() {}, invalidate() {} }).render();
    assert.equal(host.childNodes[0].style.values["background-image"].value, dynamicValue);
});
