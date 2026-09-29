import assert from "node:assert/strict";
import test from "node:test";

class FakeStyle {
    display = "";

    setProperty(name, value) {
        this[name] = value;
    }

    removeProperty(name) {
        this[name] = "";
    }
}

class FakeElement {
    childNodes = [];
    attributes = new Map();
    style = new FakeStyle();

    appendChild(child) {
        if (child.isFragment) {
            for (const fragmentChild of child.childNodes) this.appendChild(fragmentChild);
        } else {
            this.childNodes.push(child);
            child.parentNode = this;
        }
        return child;
    }

    replaceChildren(...children) {
        this.childNodes = [];
        for (const child of children) this.appendChild(child);
    }

    removeChild(child) {
        this.childNodes.splice(this.childNodes.indexOf(child), 1);
        return child;
    }

    setAttribute(name, value) {
        this.attributes.set(name, String(value));
    }

    removeAttribute(name) {
        this.attributes.delete(name);
    }

    hasAttribute(name) {
        return this.attributes.has(name);
    }

    get firstChild() {
        return this.childNodes[0] ?? null;
    }
}

class FakeFragment extends FakeElement {
    isFragment = true;
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.customElements = { define() {}, get() { return undefined; } };
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = {
    adoptedStyleSheets: [],
    baseURI: "https://example.test/",
    createComment() { return new FakeElement(); },
    createDocumentFragment() { return new FakeFragment(); },
    createElement() { return new FakeElement(); },
    createTextNode() { return new FakeElement(); }
};

const { default: createRenderEngineFactoryX, XTemplateRuntimeUtils } = await import("../render-engines/x.js");

function createEngine(state, display = "") {
    const templateRenderer = {
        dependencies: [],
        slots: [],
        render(currentState) {
            const styles = display ? { display: { value: display, priority: "" } } : null;
            return [XTemplateRuntimeUtils.createVDOM("div", null, null, styles, null, { index: 0, show: currentState.visible }, "details")];
        }
    };
    const factory = createRenderEngineFactoryX(null, {}, templateRenderer);
    factory.init();
    const host = new FakeElement();
    const engine = factory.create({ host, state, handler() {}, invalidate() {} });
    return { engine, element: () => host.firstChild };
}

test("x-show leaves an initially visible element unchanged and does not set hidden", () => {
    const state = { visible: true };
    const view = createEngine(state, "flex");

    view.engine.render();

    assert.equal(view.element().style.display, "flex");
    assert.equal(view.element().hasAttribute("hidden"), false);
});

test("x-show hides false values and restores the original empty inline display value", () => {
    const state = { visible: false };
    const view = createEngine(state);

    view.engine.render();
    assert.equal(view.element().style.display, "none");
    assert.equal(view.element().hasAttribute("hidden"), false);

    state.visible = true;
    view.engine.render();
    assert.equal(view.element().style.display, "");
});

test("x-show restores an inline display value through repeated reactive updates", () => {
    const state = { visible: false };
    const view = createEngine(state, "flex");

    view.engine.render();
    assert.equal(view.element().style.display, "none");

    state.visible = true;
    view.engine.render();
    assert.equal(view.element().style.display, "flex");

    state.visible = false;
    view.engine.render();
    assert.equal(view.element().style.display, "none");

    state.visible = true;
    view.engine.render();
    assert.equal(view.element().style.display, "flex");
    assert.equal(view.element().hasAttribute("hidden"), false);
});
