import assert from "node:assert/strict";
import test from "node:test";

class FakeElement {
    attachShadow() {
        this.shadowRoot = { adoptedStyleSheets: [] };
        return this.shadowRoot;
    }
}

class TestStyleSheet {
    text = "";

    replaceSync(text) {
        this.text = text;
    }
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = TestStyleSheet;
globalThis.customElements = {
    define() {},
    get() { return undefined; }
};
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = { adoptedStyleSheets: [], baseURI: "https://example.test/" };
globalThis.requestAnimationFrame = () => 0;

const { createComponentClassFromJsDefinition } = await import("../loaders/component-js.js");
const { createPageClassFromJsDefinition } = await import("../loaders/page-js.js");
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

    create() {
        return { mount() {}, render() {}, unmount() {} };
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

test("Component definitions accept CSS strings and reject style arrays", async () => {
    configureDefinitionLoaders();
    const styledDefinition = { meta: { id: "style-string" }, style: ".content { color: red; }" };
    const Component = await createComponentClassFromJsDefinition("style-string.js", createContext(), styledDefinition, {});
    const component = new Component();

    assert.equal(component.shadowRoot.adoptedStyleSheets.length, 1);
    assert.equal(component.shadowRoot.adoptedStyleSheets[0].text, ".content { color: red; }");
    await assert.doesNotReject(() => createComponentClassFromJsDefinition("empty-style.js", createContext(), { meta: { id: "empty-style" }, style: "" }, {}));

    const omittedStyleDefinition = { meta: { id: "omitted-style" } };
    await assert.doesNotReject(() => createComponentClassFromJsDefinition("omitted-style.js", createContext(), omittedStyleDefinition, {}));
    assert.equal(omittedStyleDefinition.style, "");
    await assert.rejects(
        () => createComponentClassFromJsDefinition("array-style.js", createContext(), { meta: { id: "array-style" }, style: [".a {}", ".b {}"] }, {}),
        /Invalid component/
    );
});

test("Page CSS strings create scoped stylesheets", async () => {
    configureDefinitionLoaders();
    document.adoptedStyleSheets = [];
    const PageClass = await createPageClassFromJsDefinition("page-style.js", createContext(), {
        meta: { id: "page-style" },
        style: ".content { color: blue; }"
    }, {});
    const page = new PageClass({ src: "/pages/style.js", context: {} });
    await page.mount({ host: { nodeName: "X-PAGE", getAttribute() { return "/pages/style.js"; }, getRootNode() { return document; } } });

    assert.equal(document.adoptedStyleSheets.length, 1);
    assert.equal(document.adoptedStyleSheets[0].text, '@scope (x-page[src="/pages/style.js"]) {.content { color: blue; };}');
});
