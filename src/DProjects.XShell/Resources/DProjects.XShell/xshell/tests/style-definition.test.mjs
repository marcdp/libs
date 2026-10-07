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
globalThis.window = { customElements: globalThis.customElements, location: { origin: "https://example.test" } };
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
    return { resourceDefinition: { moduleId: "test", modulePath: "/_assets/test" } };
}

function setStylesheets(files, requests) {
    globalThis.fetch = async url => {
        requests.push(url);
        assert.ok(files.has(url), `unexpected stylesheet: ${url}`);
        return { ok: true, async text() { return files.get(url); } };
    };
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

test("Component definition styles resolve nested imports and URLs once per class", async () => {
    configureDefinitionLoaders();
    const root = "https://example.test/_assets/test/";
    const requests = [];
    setStylesheets(new Map([
        [`${root}components/styles/theme.css`, '@import "./dark.css";\n.theme{background:url(../images/theme.png)}'],
        [`${root}components/styles/dark.css`, ".dark{color:black}"]
    ]), requests);
    const implementation = {
        meta: { id: "card" },
        style: '@import "./styles/theme.css";\n.card{background:url("./images/card.png")}'
    };

    const Component = await createComponentClassFromJsDefinition("/_assets/test/components/card.js", createContext(), implementation, {});
    assert.deepEqual(requests, [`${root}components/styles/theme.css`, `${root}components/styles/dark.css`]);
    const first = new Component();
    const second = new Component();
    assert.equal(first.shadowRoot.adoptedStyleSheets[0], second.shadowRoot.adoptedStyleSheets[0]);
    assert.equal(first.shadowRoot.adoptedStyleSheets[0].text, [
        ".dark{color:black}",
        `.theme{background:url(${root}components/images/theme.png)}`,
        `.card{background:url("${root}components/images/card.png")}`
    ].join("\n"));
    assert.equal(implementation.style, '@import "./styles/theme.css";\n.card{background:url("./images/card.png")}');
    assert.equal(Object.isFrozen(implementation), true);
    assert.equal(requests.length, 2);
});

test("Page definition styles resolve imports before scoping and keep mount cleanup", async () => {
    configureDefinitionLoaders();
    document.adoptedStyleSheets = [];
    const root = "https://example.test/_assets/test/";
    const requests = [];
    setStylesheets(new Map([
        [`${root}pages/styles/theme.css`, '@import "./dark.css";\n.theme{background:url(../images/theme.png)}'],
        [`${root}pages/styles/dark.css`, ".dark{color:black}"]
    ]), requests);
    const implementation = {
        meta: { id: "customer" },
        style: '@import "./styles/theme.css";\n.page{background:url("./images/page.png")}'
    };

    const PageClass = await createPageClassFromJsDefinition("/_assets/test/pages/customer.js", createContext(), implementation, {});
    assert.deepEqual(requests, [`${root}pages/styles/theme.css`, `${root}pages/styles/dark.css`]);
    const page = new PageClass({ src: "/pages/customer.js?id=123", context: {} });
    const host = { nodeName: "X-PAGE", getAttribute() { return "/pages/customer.js?id=123"; }, getRootNode() { return document; } };
    await page.mount({ host });
    const firstSheet = document.adoptedStyleSheets[0];
    assert.equal(firstSheet.text, `@scope (x-page[src="/pages/customer.js?id=123"]) {${[
        ".dark{color:black}",
        `.theme{background:url(${root}pages/images/theme.png)}`,
        `.page{background:url("${root}pages/images/page.png")}`
    ].join("\n")};}`);
    await page.unmount();
    assert.deepEqual(document.adoptedStyleSheets, []);
    await page.mount({ host });
    assert.equal(document.adoptedStyleSheets.length, 1);
    assert.notEqual(document.adoptedStyleSheets[0], firstSheet);
    assert.equal(requests.length, 2);
    await page.unmount();
    assert.equal(implementation.style, '@import "./styles/theme.css";\n.page{background:url("./images/page.png")}');
});

test("empty or omitted definition styles create no stylesheets and make no CSS requests", async () => {
    configureDefinitionLoaders();
    document.adoptedStyleSheets = [];
    const requests = [];
    setStylesheets(new Map(), requests);
    const EmptyComponent = await createComponentClassFromJsDefinition("/_assets/test/components/empty.js", createContext(), { meta: { id: "empty" }, style: "" }, {});
    const OmittedComponent = await createComponentClassFromJsDefinition("/_assets/test/components/omitted.js", createContext(), { meta: { id: "omitted" } }, {});
    const EmptyPage = await createPageClassFromJsDefinition("/_assets/test/pages/empty.js", createContext(), { meta: { id: "empty" }, style: "" }, {});
    const OmittedPage = await createPageClassFromJsDefinition("/_assets/test/pages/omitted.js", createContext(), { meta: { id: "omitted" } }, {});
    assert.equal(new EmptyComponent().shadowRoot.adoptedStyleSheets.length, 0);
    assert.equal(new OmittedComponent().shadowRoot.adoptedStyleSheets.length, 0);
    const host = { nodeName: "X-PAGE", getAttribute() { return "/pages/empty.js"; }, getRootNode() { return document; } };
    await new EmptyPage({ src: "/pages/empty.js", context: {} }).mount({ host });
    await new OmittedPage({ src: "/pages/omitted.js", context: {} }).mount({ host });
    assert.equal(document.adoptedStyleSheets.length, 0);
    assert.deepEqual(requests, []);
});

test("custom Component and Page styles work without modulePath", async () => {
    configureDefinitionLoaders();
    document.adoptedStyleSheets = [];
    const requests = [];
    setStylesheets(new Map([["https://example.test/custom/theme.css", ".theme{background:url(./theme.png)}"]]), requests);
    const customContext = { resourceDefinition: { moduleId: "test" } };

    const Component = await createComponentClassFromJsDefinition("https://example.test/custom/card.js", customContext, {
        meta: { id: "custom-card" }, style: '.card{background:url("./card.png")} .root{background:url(/root.png)}'
    }, {});
    assert.equal(new Component().shadowRoot.adoptedStyleSheets[0].text,
        '.card{background:url("https://example.test/custom/card.png")} .root{background:url(https://example.test/root.png)}');

    const PageClass = await createPageClassFromJsDefinition("https://example.test/custom/page.js", customContext, {
        meta: { id: "custom-page" }, style: '@import "./theme.css";\n.page{background:url("./page.png")}'
    }, {});
    assert.deepEqual(requests, ["https://example.test/custom/theme.css"]);
    const page = new PageClass({ src: "/navigation/page.js?id=1", context: {} });
    const host = { nodeName: "X-PAGE", getAttribute() { return "/navigation/page.js?id=1"; }, getRootNode() { return document; } };
    await page.mount({ host });
    assert.equal(document.adoptedStyleSheets[0].text,
        '@scope (x-page[src="/navigation/page.js?id=1"]) {.theme{background:url(https://example.test/custom/theme.png)}\n' +
        '.page{background:url("https://example.test/custom/page.png")};}');
    await page.unmount();
    assert.deepEqual(document.adoptedStyleSheets, []);
});

test("Component and Page definitions reject url: CSS references", async () => {
    configureDefinitionLoaders();
    setStylesheets(new Map(), []);
    await assert.rejects(() => createComponentClassFromJsDefinition("/_assets/test/components/card.js", createContext(), {
        meta: { id: "invalid-card" }, style: "a{background:url(url:./image.png)}"
    }, {}), /'url:' scheme is not supported in CSS resource references/);
    await assert.rejects(() => createPageClassFromJsDefinition("/_assets/test/pages/page.js", createContext(), {
        meta: { id: "invalid-page" }, style: '@import "url:./theme.css";'
    }, {}), /'url:' scheme is not supported in CSS resource references/);
});
