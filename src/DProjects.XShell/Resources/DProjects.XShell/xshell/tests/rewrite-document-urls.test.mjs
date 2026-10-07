import assert from "node:assert/strict";
import test from "node:test";
import { rewriteDocumentUrls, rewriteTemplateAttribute } from "../utils/html.js";

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

test("inline CSS uses the shared scanner and rewrites resource URLs", async () => {
    const element = new TestElement("div", { style: "color:red; background:url(images/a.png); padding:1px" });
    await rewriteDocumentUrls(createDocument(element), context);
    assert.equal(element.getAttribute("style"), `color:red; background:url(${resourceBase}images/a.png); padding:1px`);
});

test("inline CSS preserves fragment and external URLs", async () => {
    const element = new TestElement("div", { style: "filter:url(#filter); background:url(//cdn.test/a.png); mask:url(data:image/svg+xml,icon)" });
    await rewriteDocumentUrls(createDocument(element), context);
    assert.equal(element.getAttribute("style"), 'filter:url(#filter); background:url(//cdn.test/a.png); mask:url(data:image/svg+xml,icon)');
});

test("style elements rewrite resource URLs without changing other CSS", async () => {
    const style = new TestElement("style", {}, ".a { background:url(images/a.png); color: red; }");
    await rewriteDocumentUrls(createDocument(style), context);
    assert.equal(style.textContent, `.a { background:url(${resourceBase}images/a.png); color: red; }`);
});

test("style elements preserve fragment references and rewrite root URLs", async () => {
    const style = new TestElement("style", {}, ".a { filter:url(#filter); background:url(/images/a.png?v=1#part); }");
    await rewriteDocumentUrls(createDocument(style), context);
    assert.equal(style.textContent, '.a { filter:url(#filter); background:url(https://example.test/app/_assets/demo/images/a.png?v=1#part); }');
});

test("inline module imports use module paths while preserving special specifiers", async () => {
    const script = new TestElement("script", { type: "module" }, [
        'import helper from "./helper.js";',
        'import root from "/shared.js";',
        'import shell from "xshell/runtime.js";',
        'import remote from "https://cdn.test/remote.js";',
        'import protocolRelative from "//cdn.test/remote.js";'
    ].join("\n"));
    const doc = createDocument(script);
    await rewriteDocumentUrls(doc, context);
    assert.notEqual(doc.elements[0], script);
    assert.equal(doc.elements[0].textContent, [
        `import helper from "${resourceBase}helper.js";`,
        'import root from "https://example.test/app/_assets/demo/shared.js";',
        'import shell from "xshell/runtime.js";',
        'import remote from "https://cdn.test/remote.js";',
        'import protocolRelative from "//cdn.test/remote.js";'
    ].join("\n"));
});

test("inline module imports resolve parent paths and retain query strings and fragments", async () => {
    const script = new TestElement("script", { type: "module" }, 'import parent from "../shared.js?v=1#part";');
    const doc = createDocument(script);
    await rewriteDocumentUrls(doc, context);
    assert.equal(doc.elements[0].textContent, 'import parent from "https://example.test/app/_assets/demo/shared.js?v=1#part";');
});

test("img srcset rewrites density candidates independently", async () => {
    const img = new TestElement("img", { srcset: "small.png 1x, large.png 2x" });
    await rewriteDocumentUrls(createDocument(img), context);
    assert.equal(img.getAttribute("srcset"), `${resourceBase}small.png 1x, ${resourceBase}large.png 2x`);
});

test("source srcset rewrites width candidates independently", async () => {
    const source = new TestElement("source", { srcset: "small.png 480w, large.png 960w" });
    await rewriteDocumentUrls(createDocument(source), context);
    assert.equal(source.getAttribute("srcset"), `${resourceBase}small.png 480w, ${resourceBase}large.png 960w`);
});

test("srcset preserves commas inside data URLs", async () => {
    const img = new TestElement("img", { srcset: "data:image/png;base64,AAAA 1x, large.png 2x" });
    await rewriteDocumentUrls(createDocument(img), context);
    assert.equal(img.getAttribute("srcset"), `data:image/png;base64,AAAA 1x, ${resourceBase}large.png 2x`);
});

test("X Template srcset attributes use the same candidate rewriting", () => {
    createDocument();
    assert.equal(rewriteTemplateAttribute("img", {}, "srcset", "small.png 1x, large.png 2x", context),
        `${resourceBase}small.png 1x, ${resourceBase}large.png 2x`);
});

test("resource, navigation, virtual navigation, and qualified URLs retain their rules", async () => {
    const img = new TestElement("img", { src: "images/a.png" });
    const external = new TestElement("img", { src: "https://cdn.test/a.png" });
    const anchor = new TestElement("a", { href: "details.html" });
    const virtualAnchor = new TestElement("x-anchor", { href: "/dashboard" });
    await rewriteDocumentUrls(createDocument(img, external, anchor, virtualAnchor), context);
    assert.equal(img.getAttribute("src"), `${resourceBase}images/a.png`);
    assert.equal(external.getAttribute("src"), "https://cdn.test/a.png");
    assert.equal(anchor.getAttribute("href"), "#!/_assets/demo/pages/details.html");
    assert.equal(virtualAnchor.getAttribute("href"), "/_assets/demo/dashboard");
});

test("resource attributes resolve local URLs and preserve external and fragment URLs", async () => {
    const cases = [
        ["img", "src", "./photo.png?v=1#part", `${resourceBase}photo.png?v=1#part`],
        ["img", "src", "../shared.png?v=1#part", "https://example.test/app/_assets/demo/shared.png?v=1#part"],
        ["source", "srcset", "/large.png 2x, small.png 1x", `https://example.test/app/_assets/demo/large.png 2x, ${resourceBase}small.png 1x`],
        ["link", "href", "/theme.css", "https://example.test/app/_assets/demo/theme.css"],
        ["iframe", "src", "//cdn.test/embed", "//cdn.test/embed"],
        ["object", "data", "blob:https://example.test/id", "blob:https://example.test/id"],
        ["img", "src", "data:image/png;base64,AAAA", "data:image/png;base64,AAAA"],
        ["img", "src", "#sprite", "#sprite"]
    ];
    for (const [tag, attr, value, expected] of cases) {
        const element = new TestElement(tag, { [attr]: value });
        await rewriteDocumentUrls(createDocument(element), context);
        assert.equal(element.getAttribute(attr), expected, `${tag}[${attr}] = ${value}`);
    }
});

test("navigation links retain Page routes and fragment navigation", async () => {
    const links = [
        ["./details.html?v=1#part", "#!/_assets/demo/pages/details.html?v=1#part"],
        ["../index.html?v=1#part", "#!/_assets/demo/index.html?v=1#part"],
        ["#section", "#!/_assets/demo/pages/page.html#section"],
        ["//cdn.test/page", "//cdn.test/page"]
    ];
    for (const [href, expected] of links) {
        const anchor = new TestElement("a", { href });
        await rewriteDocumentUrls(createDocument(anchor), context);
        assert.equal(anchor.getAttribute("href"), expected);
    }
});

test("runtime template resource URLs reject the configuration-only url: scheme", async () => {
    const cases = [
        new TestElement("img", { src: "url:./image.png" }),
        new TestElement("a", { href: "url:./page.html" }),
        new TestElement("video", { poster: "url:./poster.png" }),
        new TestElement("form", { action: "url:./submit" }),
        new TestElement("button", { formaction: "url:./submit" }),
        new TestElement("img", { srcset: "small.png 1x, URL:./large.png 2x" }),
        new TestElement("div", { style: "background:url(url:./image.png)" }),
        new TestElement("style", {}, "a{background:url('url:./image.png')}")
    ];
    for (const element of cases) {
        await assert.rejects(() => rewriteDocumentUrls(createDocument(element), context), /'url:' scheme is not supported/);
    }
    createDocument();
    assert.throws(() => rewriteTemplateAttribute("img", {}, "src", "url:./image.png", context), /'url:' scheme is not supported/);
    assert.throws(() => rewriteTemplateAttribute("img", {}, "srcset", "small.png 1x, url:./large.png 2x", context), /'url:' scheme is not supported/);
});

test("runtime template imports reject url: while ordinary text remains untouched", async () => {
    const script = new TestElement("script", { type: "module" }, 'import x from "url:./module.js";');
    await assert.rejects(() => rewriteDocumentUrls(createDocument(script), context), /'url:' scheme is not supported/);

    const plain = new TestElement("p", { title: "url:./literal" }, "url:./plain text");
    const ordinaryScript = new TestElement("script", { type: "module" }, 'const text = "url:./literal";');
    const doc = createDocument(plain, ordinaryScript);
    await rewriteDocumentUrls(doc, context);
    assert.equal(plain.getAttribute("title"), "url:./literal");
    assert.equal(plain.textContent, "url:./plain text");
    assert.equal(doc.elements[1].textContent, 'const text = "url:./literal";');
});

test("runtime templates resolve app: resources against the application base", async () => {
    const img = new TestElement("img", { src: "app:/images/logo.png" });
    const style = new TestElement("style", {}, "a{background:url(app:/images/background.png)}");
    await rewriteDocumentUrls(createDocument(img, style), context);
    assert.equal(img.getAttribute("src"), "https://example.test/app/images/logo.png");
    assert.equal(style.textContent, 'a{background:url(https://example.test/app/images/background.png)}');
    assert.throws(() => rewriteTemplateAttribute("img", {}, "src", "app:../../outside.png", context), /escapes the application root/);
});
