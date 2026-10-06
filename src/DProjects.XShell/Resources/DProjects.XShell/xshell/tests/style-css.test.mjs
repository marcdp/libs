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

test("external CSS rewrites local url() values from the module root", async () => {
    const css = [
        "a{background:url(relative.png)}",
        "b{background:url(/root.png)}",
        "c{background:url(image.png?v=1#part)}",
        "d{filter:url(#filter)}",
        "e{background:url(https://cdn.test/image.png)}",
        "f{background:url(//cdn.test/image.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n");
    const requests = [];
    setStylesheets(new Map([[`${moduleRoot}styles/main.css`, css]]), requests);

    const sheet = await new LoaderStyleCss().load("styles/main.css", context);

    assert.deepEqual(requests, [`${moduleRoot}styles/main.css`]);
    assert.equal(sheet.css, [
        `a{background:url(${moduleRoot}relative.png)}`,
        `b{background:url(${moduleRoot}root.png)}`,
        `c{background:url(${moduleRoot}image.png?v=1#part)}`,
        "d{filter:url(#filter)}",
        "e{background:url(https://cdn.test/image.png)}",
        "f{background:url(//cdn.test/image.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n"));
});

test("CSS @import resolves relative and root URLs from the module root", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleRoot}styles/main.css`, '@import "theme.css";\n@import url(/reset.css);\nmain{color:black}'],
        [`${moduleRoot}theme.css`, "theme{background:url(theme.png)}"],
        [`${moduleRoot}reset.css`, "reset{background:url(/reset.png)}"]
    ]), requests);

    const sheet = await new LoaderStyleCss().load("styles/main.css", context);

    assert.deepEqual(requests, [`${moduleRoot}styles/main.css`, `${moduleRoot}theme.css`, `${moduleRoot}reset.css`]);
    assert.equal(sheet.css, `theme{background:url(${moduleRoot}theme.png)}\nreset{background:url(${moduleRoot}reset.png)}\nmain{color:black}`);
});
