import assert from "node:assert/strict";
import test from "node:test";
import LoaderStyleCss from "../loaders/style-css.js";

const moduleRoot = "https://example.test/_assets/demo/";
const context = { resourceDefinition: { modulePath: "/_assets/demo" } };

class TestStyleSheet {
    async replace(css) { this.css = css; }
}

function setStylesheets(files, requests) {
    globalThis.window = { location: { origin: "https://example.test" } };
    globalThis.CSSStyleSheet = TestStyleSheet;
    globalThis.fetch = async url => {
        requests.push(url);
        assert.ok(files.has(url), `unexpected stylesheet: ${url}`);
        return { ok: true, async text() { return files.get(url); } };
    };
}

test("CSS url() values resolve against their containing stylesheet and preserve absolute or special URLs", async () => {
    const css = [
        "a{background:url(../icons/x-file.svg)}",
        "b{background:url(/root.png)}",
        "c{background:url(image.png?v=1#part)}",
        "d{filter:url(#filter)}",
        "e{background:url(https://cdn.test/image.png)}",
        "f{background:url(//cdn.test/image.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n");
    const requests = [];
    setStylesheets(new Map([[`${moduleRoot}styles/index.css`, css]]), requests);

    const sheet = await new LoaderStyleCss().load("/_assets/demo/styles/index.css", context);

    assert.deepEqual(requests, [`${moduleRoot}styles/index.css`]);
    assert.equal(sheet.css, [
        `a{background:url(${moduleRoot}icons/x-file.svg)}`,
        `b{background:url(${moduleRoot}root.png)}`,
        `c{background:url(${moduleRoot}styles/image.png?v=1#part)}`,
        "d{filter:url(#filter)}",
        "e{background:url(https://cdn.test/image.png)}",
        "f{background:url(//cdn.test/image.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n"));
});

test("absolute stylesheet URLs inside the module namespace retain module CSS semantics", async () => {
    const moduleRoot = "https://example.test/prefix/_assets/demo/1.0.0.dev/";
    const stylesheetUrl = `${moduleRoot}styles/index.css`;
    const context = {
        appBasePath: "/prefix",
        resourceDefinition: {
            modulePath: "/_assets/demo/1.0.0.dev"
        }
    };
    const requests = [];
    setStylesheets(new Map([[stylesheetUrl, "a{background:url(/icons/test.svg)}"]]), requests);

    const sheet = await new LoaderStyleCss().load(`${moduleRoot}styles/index.css`, context);

    assert.deepEqual(requests, ["https://example.test/prefix/_assets/demo/1.0.0.dev/styles/index.css"]);
    assert.equal(sheet.css, `a{background:url(${moduleRoot}icons/test.svg)}`);
    assert.ok(!sheet.css.includes("https://example.test/icons/test.svg"));
});

test("CSS @import and nested stylesheet URLs each use the containing stylesheet as their base", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleRoot}styles/index.css`, '@import "./theme.css";\n@import url(/reset.css);\nmain{color:black}'],
        [`${moduleRoot}styles/theme.css`, '@import "./themes/dark.css";\ntheme{background:url(../icons/theme.svg)}'],
        [`${moduleRoot}styles/themes/dark.css`, "dark{background:url(../images/dark.png)}"],
        [`${moduleRoot}reset.css`, "reset{background:url(/reset.png)}"]
    ]), requests);

    const sheet = await new LoaderStyleCss().load("styles/index.css", context);

    assert.deepEqual(requests, [
        `${moduleRoot}styles/index.css`,
        `${moduleRoot}styles/theme.css`,
        `${moduleRoot}styles/themes/dark.css`,
        `${moduleRoot}reset.css`
    ]);
    assert.equal(sheet.css, [
        `dark{background:url(${moduleRoot}styles/images/dark.png)}`,
        `theme{background:url(${moduleRoot}icons/theme.svg)}`,
        `reset{background:url(${moduleRoot}reset.png)}`,
        "main{color:black}"
    ].join("\n"));
});

test("CSS @import media qualifiers remain wrapped and unsupported qualifiers remain rejected", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleRoot}styles/index.css`, '@import "./print.css" print;'],
        [`${moduleRoot}styles/print.css`, "p{color:black}"]
    ]), requests);

    const sheet = await new LoaderStyleCss().load("styles/index.css", context);
    assert.equal(sheet.css, "@media print {\np{color:black}\n}");

    for (const qualifier of ["layer(theme)", "supports(display: grid)"]) {
        setStylesheets(new Map([
            [`${moduleRoot}styles/index.css`, `@import "./print.css" ${qualifier};`],
            [`${moduleRoot}styles/print.css`, "p{color:black}"]
        ]), []);
        await assert.rejects(() => new LoaderStyleCss().load("styles/index.css", context), /Unsupported CSS @import qualifiers/);
    }
});

test("CSS @import cycles are rejected", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleRoot}styles/index.css`, '@import "./theme.css";'],
        [`${moduleRoot}styles/theme.css`, '@import "./index.css";']
    ]), requests);

    await assert.rejects(() => new LoaderStyleCss().load("styles/index.css", context), /Circular CSS @import detected/);
    assert.deepEqual(requests, [`${moduleRoot}styles/index.css`, `${moduleRoot}styles/theme.css`]);
});

test("custom external stylesheets work without modulePath", async () => {
    const requests = [];
    setStylesheets(new Map([["https://example.test/custom/site.css", "a{background:url(./icon.png)} b{background:url(/root.png)}"]]), requests);

    const sheet = await new LoaderStyleCss().load("https://example.test/custom/site.css", { resourceDefinition: {} });

    assert.deepEqual(requests, ["https://example.test/custom/site.css"]);
    assert.equal(sheet.css, "a{background:url(https://example.test/custom/icon.png)} b{background:url(https://example.test/root.png)}");
});

test("external stylesheets reject url: resource references", async () => {
    setStylesheets(new Map([[moduleRoot + "styles/index.css", "a{background:url(url:./image.png)}"]]), []);

    await assert.rejects(() => new LoaderStyleCss().load("styles/index.css", context), /'url:' scheme is not supported in CSS resource references/);
});
