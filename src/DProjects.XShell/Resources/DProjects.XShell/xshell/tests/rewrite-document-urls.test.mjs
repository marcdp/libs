import assert from "node:assert/strict";
import test from "node:test";
import { rewriteDocumentUrls, rewriteTemplateAttribute } from "../utils/rewriteDocumentUrls.js";

class TestElement {
    constructor(tag, attributes = {}, textContent = "") {
        this.tag = tag;
        this.attributes = new Map(Object.entries(attributes));
        this.textContent = textContent;
    }

    getAttribute(name) { return this.attributes.get(name) ?? null; }
    setAttribute(name, value) { this.attributes.set(name, value); }
    get src() { return this.getAttribute("src") || ""; }
    matches(selector) {
        if (selector == "[style]") return this.attributes.has("style");
        if (selector == 'script[type="module"]') return this.tag == "script" && this.getAttribute("type") == "module";
        const [tag, condition] = selector.split("[");
        if (this.tag != tag) return false;
        if (!condition) return true;
        if (condition == "formaction]") return this.attributes.has("formaction");
        if (condition == "type=image]") return this.getAttribute("type") == "image";
        return false;
    }
    replaceWith(replacement) { this.document.elements[this.document.elements.indexOf(this)] = replacement; }
}

function createDocument(...elements) {
    const doc = {
        elements,
        querySelectorAll(selector) { return this.elements.filter(element => element.matches(selector)); },
        createElement(tag) { return new TestElement(tag); }
    };
    for (const element of elements) element.document = doc;
    globalThis.document = doc;
    globalThis.window = { location: { origin: "https://example.test" } };
    return doc;
}

const context = {
    appBasePath: "https://example.test/app",
    resourceDefinition: { modulePath: "/_assets/demo" },
    resourcePath: "/_assets/demo/pages/page.html",
    navigationMode: "hash",
    navigationHashPrefix: "#!"
};
const resourceBase = "https://example.test/app/_assets/demo/pages/";

test("inline CSS rewrites resource URLs and preserves other declarations", () => {
    const element = new TestElement("div", { style: "color:red; background:url(images/a.png); padding:1px" });
    rewriteDocumentUrls(createDocument(element), context);
    assert.equal(element.getAttribute("style"), `color:red; background:url("${resourceBase}images/a.png"); padding:1px`);
});

test("style elements rewrite resource URLs without changing other CSS", () => {
    const style = new TestElement("style", {}, ".a { background:url(images/a.png); color: red; }");
    rewriteDocumentUrls(createDocument(style), context);
    assert.equal(style.textContent, `.a { background:url("${resourceBase}images/a.png"); color: red; }`);
});

test("inline module imports use module paths while preserving special specifiers", () => {
    const script = new TestElement("script", { type: "module" }, [
        'import helper from "./helper.js";',
        'import root from "/shared.js";',
        'import shell from "xshell/runtime.js";',
        'import remote from "https://cdn.test/remote.js";'
    ].join("\n"));
    const doc = createDocument(script);
    rewriteDocumentUrls(doc, context);
    assert.notEqual(doc.elements[0], script);
    assert.equal(doc.elements[0].textContent, [
        `import helper from "${resourceBase}helper.js";`,
        'import root from "https://example.test/app/_assets/demo/shared.js";',
        'import shell from "xshell/runtime.js";',
        'import remote from "https://cdn.test/remote.js";'
    ].join("\n"));
});

test("img srcset rewrites density candidates independently", () => {
    const img = new TestElement("img", { srcset: "small.png 1x, large.png 2x" });
    rewriteDocumentUrls(createDocument(img), context);
    assert.equal(img.getAttribute("srcset"), `${resourceBase}small.png 1x, ${resourceBase}large.png 2x`);
});

test("source srcset rewrites width candidates independently", () => {
    const source = new TestElement("source", { srcset: "small.png 480w, large.png 960w" });
    rewriteDocumentUrls(createDocument(source), context);
    assert.equal(source.getAttribute("srcset"), `${resourceBase}small.png 480w, ${resourceBase}large.png 960w`);
});

test("srcset preserves commas inside data URLs", () => {
    const img = new TestElement("img", { srcset: "data:image/png;base64,AAAA 1x, large.png 2x" });
    rewriteDocumentUrls(createDocument(img), context);
    assert.equal(img.getAttribute("srcset"), `data:image/png;base64,AAAA 1x, ${resourceBase}large.png 2x`);
});

test("X Template srcset attributes use the same candidate rewriting", () => {
    createDocument();
    assert.equal(rewriteTemplateAttribute("img", {}, "srcset", "small.png 1x, large.png 2x", context),
        `${resourceBase}small.png 1x, ${resourceBase}large.png 2x`);
});

test("resource, navigation, virtual navigation, and qualified URLs retain their rules", () => {
    const img = new TestElement("img", { src: "images/a.png" });
    const external = new TestElement("img", { src: "https://cdn.test/a.png" });
    const anchor = new TestElement("a", { href: "details.html" });
    const virtualAnchor = new TestElement("x-anchor", { href: "/dashboard" });
    rewriteDocumentUrls(createDocument(img, external, anchor, virtualAnchor), context);
    assert.equal(img.getAttribute("src"), `${resourceBase}images/a.png`);
    assert.equal(external.getAttribute("src"), "https://cdn.test/a.png");
    assert.equal(anchor.getAttribute("href"), "#!/_assets/demo/pages/details.html");
    assert.equal(virtualAnchor.getAttribute("href"), "/_assets/demo/dashboard");
});
