import assert from "node:assert/strict";
import test from "node:test";

class FakeNode {
    constructor(tag = "div") {
        this.tagName = tag;
        this.childNodes = [];
        this.attributes = new Map();
        this.style = { display: "", setProperty() {}, removeProperty() {} };
    }

    get firstChild() { return this.childNodes[0] ?? null; }
    get lastChild() { return this.childNodes.at(-1) ?? null; }
    appendChild(child) {
        if (child.isFragment) {
            for (const node of [...child.childNodes]) this.appendChild(node);
            return child;
        }
        child.parentNode?.removeChild(child);
        this.childNodes.push(child);
        child.parentNode = this;
        return child;
    }
    insertBefore(child, before) {
        child.parentNode?.removeChild(child);
        const index = before ? this.childNodes.indexOf(before) : -1;
        this.childNodes.splice(index < 0 ? this.childNodes.length : index, 0, child);
        child.parentNode = this;
        return child;
    }
    removeChild(child) {
        const index = this.childNodes.indexOf(child);
        if (index < 0) throw new Error("Node is not a child");
        this.childNodes.splice(index, 1);
        child.parentNode = null;
        return child;
    }
    replaceChild(next, old) {
        const index = this.childNodes.indexOf(old);
        if (index < 0) throw new Error("Node is not a child");
        this.removeChild(old);
        this.insertBefore(next, this.childNodes[index]);
        return old;
    }
    replaceChildren(...children) {
        for (const child of [...this.childNodes]) this.removeChild(child);
        for (const child of children) this.appendChild(child);
    }
    querySelectorAll() {
        const elements = [];
        for (const child of this.childNodes) {
            if (!child.isFragment && !child.isText) elements.push(child);
            elements.push(...child.querySelectorAll());
        }
        return elements;
    }
    setAttribute(name, value) { this.attributes.set(name, String(value)); }
    removeAttribute(name) { this.attributes.delete(name); }
    get innerHTML() { return ""; }
    set innerHTML(value) { this.replaceChildren(); }
    get textContent() { return ""; }
    set textContent(value) { this.replaceChildren(); }
    addEventListener() {}
}

class ComponentNode extends FakeNode {
    static isXShellComponent = true;
    unloadCalls = 0;
    unloadCount = 0;
    unload() {
        this.unloadCalls++;
        if (!this.unloadCount) this.unloadCount++;
    }
}

globalThis.HTMLElement = FakeNode;
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.customElements = { define() {}, get() {} };
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = {
    adoptedStyleSheets: [], baseURI: "https://example.test/",
    createDocumentFragment() { const fragment = new FakeNode(); fragment.isFragment = true; return fragment; },
    createComment() { const comment = new FakeNode("#comment"); comment.isText = true; return comment; },
    createTextNode() { const text = new FakeNode("#text"); text.isText = true; return text; },
    createElement(tag) { return tag.startsWith("x-") ? new ComponentNode(tag) : new FakeNode(tag); }
};

const { default: createRenderEngineFactoryX, XTemplateRuntimeUtils: utils } = await import("../render-engines/x.js");
const { RenderEngineHtml } = await import("../render-engines/html.js");
const { unloadComponents } = await import("../utils/components.js");
const vnode = (tag, index = 0, options = {}, children = []) => utils.createVDOM(tag, null, null, null, null, { index, ...options }, children);

function createXEngine(render, state = {}) {
    const factory = createRenderEngineFactoryX(null, {}, { render, dependencies: [], slots: [] });
    factory.init();
    const host = new FakeNode("#shadow-root");
    const engine = factory.create({ host, state, handler() {}, invalidate() {} });
    engine.mount();
    return { host, engine, state };
}

test("XTemplate conditional removal and tag replacement unload discarded Components", () => {
    const view = createXEngine(state => state.visible ? [vnode(state.tag)] : [], { visible: true, tag: "x-old" });
    view.engine.render();
    const first = view.host.firstChild;
    view.state.visible = false;
    view.engine.render();
    assert.equal(first.unloadCalls, 1);
    assert.equal(view.host.childNodes.length, 0);

    view.state.visible = true;
    view.engine.render();
    const second = view.host.firstChild;
    view.state.tag = "x-next";
    view.engine.render();
    assert.equal(second.unloadCalls, 1);
    assert.equal(view.host.firstChild.tagName, "x-next");
});

test("keyed XTemplate removal unloads once while reordering preserves instances", () => {
    const render = state => [
        vnode("#comment", 0, { forType: "key" }, "x-for-start"),
        ...state.keys.map((key, index) => vnode("x-child", index + 1, { key })),
        vnode("#comment", state.keys.length + 1, {}, "x-for-end")
    ];
    const view = createXEngine(render, { keys: ["a", "b"] });
    view.engine.render();
    const [start, first, second] = view.host.childNodes;
    view.state.keys = ["b", "a"];
    view.engine.render();
    assert.equal(view.host.childNodes[0], start);
    assert.deepEqual(view.host.childNodes.slice(1, 3), [second, first]);
    assert.equal(first.unloadCalls, 0);
    assert.equal(second.unloadCalls, 0);

    view.state.keys = ["b"];
    view.engine.render();
    assert.equal(first.unloadCalls, 1);
    assert.equal(second.unloadCalls, 0);
    view.engine.unmount();
    assert.equal(first.unloadCalls, 1);
    assert.equal(second.unloadCalls, 1);
});

test("XTemplate node format retains reused children and unloads only removed children", () => {
    const kept = new ComponentNode("x-kept");
    const removed = new ComponentNode("x-removed");
    const view = createXEngine(state => [vnode("div", 0, { format: "node" }, state.children)], { children: [kept, removed] });
    view.engine.render();
    view.state.children = [kept];
    view.engine.render();
    assert.equal(kept.unloadCalls, 0);
    assert.equal(removed.unloadCalls, 1);
    assert.deepEqual(view.host.firstChild.childNodes, [kept]);
});

test("both render engines unload child Components before clearing their own trees", () => {
    const x = createXEngine(() => [vnode("div", 0, {}, [vnode("x-child")])]);
    x.engine.render();
    const xChild = x.host.firstChild.firstChild;
    xChild.unload = () => { assert.equal(xChild.parentNode, x.host.firstChild); xChild.unloadCount++; };
    x.engine.unmount();
    assert.equal(xChild.unloadCount, 1);
    assert.equal(x.host.childNodes.length, 0);

    const htmlHost = new FakeNode("#shadow-root");
    const template = { cloneNode() { return { content: document.createDocumentFragment() }; } };
    const html = new RenderEngineHtml({ host: htmlHost, template, state: {} });
    html.mount();
    const htmlChild = htmlHost.appendChild(new ComponentNode("x-child"));
    htmlChild.unload = () => { assert.equal(htmlChild.parentNode, htmlHost); htmlChild.unloadCount++; };
    html.unmount();
    assert.equal(htmlChild.unloadCount, 1);
    assert.equal(htmlHost.childNodes.length, 0);
});

test("unload snapshots light DOM and does not enter Component shadow roots or arbitrary custom elements", () => {
    const root = new FakeNode();
    const parent = root.appendChild(new ComponentNode("x-parent"));
    const child = parent.appendChild(new ComponentNode("x-child"));
    const shadowChild = new ComponentNode("x-shadow-child");
    parent.shadowRoot = new FakeNode("#shadow-root");
    parent.shadowRoot.appendChild(shadowChild);
    const unrelated = root.appendChild(new FakeNode("foreign-element"));
    unrelated.unload = () => { throw new Error("unrelated unload must not run"); };
    parent.unload = () => {
        if (parent.unloadCount) return;
        parent.unloadCount++;
        parent.unloadCalls++;
        parent.replaceChildren();
    };

    unloadComponents(root);
    unloadComponents(root);
    assert.equal(parent.unloadCalls, 1);
    assert.equal(child.unloadCalls, 1);
    assert.equal(shadowChild.unloadCalls, 0);
});

test("rejected asynchronous unload is reported without escaping synchronous rendering", async (t) => {
    const errors = [];
    t.mock.method(console, "error", (...args) => errors.push(args));
    const child = new ComponentNode("x-child");
    child.unload = async () => { throw new Error("unload rejected"); };

    assert.doesNotThrow(() => unloadComponents(child));
    await Promise.resolve();
    assert.equal(errors.length, 1);
    assert.match(errors[0][1].message, /unload rejected/);
});
