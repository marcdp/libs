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
    navigationMode: "hash"
};
const resourceBase = "https://example.test/app/_assets/demo/pages/";

test("every built-in HTML URL rule rewrites a server-logical URL once", () => {
    const cases = [
        ["a", "href", "navigation"], ["area", "href", "navigation"], ["form", "action", "navigation"],
        ["button", "formaction", "navigation"], ["x-page", "src", "virtual_navigation"], ["x-anchor", "href", "virtual_navigation"],
        ["img", "src", "resource"], ["img", "srcset", "resource"], ["source", "src", "resource"],
        ["source", "srcset", "resource"], ["link", "href", "resource"], ["script", "src", "resource"],
        ["iframe", "src", "resource"], ["video", "poster", "resource"], ["video", "src", "resource"],
        ["audio", "src", "resource"], ["embed", "src", "resource"], ["object", "data", "resource"],
        ["input", "src", "resource"], ["track", "src", "resource"]
    ];
    for (const [tag, attr, type] of cases) {
        const element = new TestElement(tag, { [attr]: "/pages/images/a.png", ...(tag === "input" ? { type: "image" } : {}) });
        rewriteDocumentUrls(createDocument(element), context);
        const expected = type === "resource" ? "https://example.test/app/_assets/demo/pages/images/a.png"
            : type === "virtual_navigation" ? "/_assets/demo/pages/images/a.png" : "#!/_assets/demo/pages/images/a.png";
        assert.equal(element.getAttribute(attr), expected, `${tag}[${attr}]`);
    }
});

test("unowned URL-looking attributes remain authored through the runtime", () => {
    const cases = [
        new TestElement("use", { href: "./icons.svg#edit" }), new TestElement("div", { href: "./x", src: "./x", poster: "./x", action: "./x", formaction: "./x" }),
        new TestElement("input", { type: "text", src: "./x" }), new TestElement("custom-element", { href: "./x", srcset: "./x 1x" }),
        new TestElement("div", { "data-value": "app:/foo", href: "url:./x" })
    ];
    const before = cases.map(element => [...element.attributes]);
    rewriteDocumentUrls(createDocument(...cases), context);
    cases.forEach((element, index) => assert.deepEqual([...element.attributes], before[index]));
});

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

test("style elements resolve quoted imports through the CSS scanner", () => {
    const style = new TestElement("style", {}, '@import "./theme.css"; .a{background:url(./image.png)}');
    rewriteDocumentUrls(createDocument(style), context);
    assert.equal(style.textContent, `@import "${resourceBase}theme.css"; .a{background:url(${resourceBase}image.png)}`);
    for (const scheme of ["app:", "APP:", "url:", "URL:"]) {
        assert.throws(() => rewriteDocumentUrls(createDocument(new TestElement("style", {}, `@import "${scheme}/theme.css";`)), context), /scheme is not supported/);
    }
});

test("inline module script bodies remain exactly authored", () => {
    const source = [
        'import realLooking from "./module.js";',
        '',
        '/*',
        'import commented from "./commented.js";',
        '*/',
        '',
        'const sample = `',
        'import templateText from "./template.js";',
        '`;'
    ].join("\n");
    const script = new TestElement("script", { type: "module" }, source);
    rewriteDocumentUrls(createDocument(script), context);
    assert.equal(script.textContent, source);
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

test("path navigation respects the application base form", () => {
    for (const [appBasePath, expected] of [
        ["/app", "/app/_assets/demo/pages/details"],
        ["https://example.test/app", "https://example.test/app/_assets/demo/pages/details"]
    ]) {
        const anchor = new TestElement("a", { href: "./details" });
        rewriteDocumentUrls(createDocument(anchor), { ...context, appBasePath, navigationMode: "path" });
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
        assert.throws(() => rewriteDocumentUrls(createDocument(element), context), /'url:' scheme is not supported/);
    }
    createDocument();
    assert.throws(() => rewriteTemplateAttribute("img", {}, "src", "url:./image.png", context), /'url:' scheme is not supported/);
    assert.throws(() => rewriteTemplateAttribute("img", {}, "srcset", "small.png 1x, url:./large.png 2x", context), /'url:' scheme is not supported/);
});

test("runtime templates reject app: and url: in all static URL locations", () => {
    for (const scheme of ["app:", "APP:", "url:", "URL:"]) {
        for (const element of [
            new TestElement("img", { src: scheme + "/image.png" }),
            new TestElement("a", { href: scheme + "/page" }),
            new TestElement("x-page", { src: scheme + "/page" }),
            new TestElement("img", { srcset: `small.png 1x, ${scheme}/large.png 2x` }),
            new TestElement("div", { style: `background:url(${scheme}/image.png)` }),
            new TestElement("style", {}, `a{background:url(${scheme}/image.png)}`)
        ]) assert.throws(() => rewriteDocumentUrls(createDocument(element), context), /scheme is not supported/);
        createDocument();
        assert.throws(() => rewriteTemplateAttribute("img", {}, "src", scheme + "/image.png", context), /scheme is not supported/);
    }
});

test("custom declaring URLs use ordinary URL semantics", () => {
    const custom = { resourceDefinition: {}, resourcePath: "https://cdn.example.com/widgets/card.js", appBasePath: "/app", navigationMode: "hash" };
    for (const [value, expected] of [
        ["./a.png", "https://cdn.example.com/widgets/a.png"],
        ["../a.png", "https://cdn.example.com/a.png"],
        ["/a.png", "https://cdn.example.com/a.png"]
    ]) {
        const img = new TestElement("img", { src: value });
        rewriteDocumentUrls(createDocument(img), custom);
        assert.equal(img.getAttribute("src"), expected);
    }
    const anchor = new TestElement("a", { href: "./details" });
    rewriteDocumentUrls(createDocument(anchor), custom);
    assert.equal(anchor.getAttribute("href"), "https://cdn.example.com/widgets/details");
});

test("module URL resolution keeps colons in suffixes and rejects root traversal", () => {
    for (const [value, expected] of [
        ["./image.png?time=10:30", `${resourceBase}image.png?time=10:30`],
        ["./image.png#state:active", `${resourceBase}image.png#state:active`]
    ]) {
        const img = new TestElement("img", { src: value });
        rewriteDocumentUrls(createDocument(img), context);
        assert.equal(img.getAttribute("src"), expected);
    }
    assert.throws(() => rewriteDocumentUrls(createDocument(new TestElement("img", { src: "../../../outside.png" })), context), /escapes the module root/);
});

test("server-normalized logical module URLs map once to runtime URLs", () => {
    const img = new TestElement("img", { src: "/pages/images/a.png" });
    rewriteDocumentUrls(createDocument(img), context);
    assert.equal(img.getAttribute("src"), "https://example.test/app/_assets/demo/pages/images/a.png");
});

test("video source and poster are rewritten", () => {
    const video = new TestElement("video", { src: "./movie.mp4", poster: "./poster.png" });
    rewriteDocumentUrls(createDocument(video), context);
    assert.equal(video.getAttribute("src"), `${resourceBase}movie.mp4`);
    assert.equal(video.getAttribute("poster"), `${resourceBase}poster.png`);
});
