import assert from "node:assert/strict";
import test from "node:test";
import { processStyle } from "../utils/style.js";

const moduleRoot = "https://example.test/console/_assets/demo/";
const context = {
    appBasePath: "/console",
    resourceDefinition: { modulePath: "/_assets/demo" }
};

function setStylesheets(files, requests) {
    globalThis.window = { location: { origin: "https://example.test" } };
    globalThis.fetch = async url => {
        requests.push(url);
        assert.ok(files.has(url), `unexpected stylesheet: ${url}`);
        return { ok: true, async text() { return files.get(url); } };
    };
}

test("inline CSS uses its declaring source and each nested import uses its own source", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleRoot}pages/styles/theme.css`, '@import "./variants/dark.css";\n.icon{background:url(../images/icon.png)}'],
        [`${moduleRoot}pages/styles/variants/dark.css`, "dark{background:url(../../images/dark.png)}"]
    ]), requests);

    const css = await processStyle({
        src: "/console/_assets/demo/pages/customer.js",
        context,
        css: '@import "./styles/theme.css";\n.page{background:url("./images/page.png")}'
    });

    assert.deepEqual(requests, [`${moduleRoot}pages/styles/theme.css`, `${moduleRoot}pages/styles/variants/dark.css`]);
    assert.equal(css, [
        `dark{background:url(${moduleRoot}pages/images/dark.png)}`,
        `.icon{background:url(${moduleRoot}pages/images/icon.png)}`,
        `.page{background:url("${moduleRoot}pages/images/page.png")}`
    ].join("\n"));
});

test("omitted CSS fetches the root, while explicit empty CSS never fetches it", async () => {
    const requests = [];
    setStylesheets(new Map([[`${moduleRoot}styles/site.css`, "a{background:url(image.png)}"]]), requests);

    assert.equal(await processStyle({ src: "styles/site.css", context }), `a{background:url(${moduleRoot}styles/image.png)}`);
    assert.equal(await processStyle({ src: "styles/site.css", context, css: "" }), "");
    assert.deepEqual(requests, [`${moduleRoot}styles/site.css`]);
});

test("inline imports preserve media qualifiers and reject unsupported qualifiers and cycles", async () => {
    const requests = [];
    setStylesheets(new Map([[`${moduleRoot}pages/print.css`, "p{color:black}"]]), requests);
    assert.equal(await processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: '@import "./print.css" print;'
    }), "@media print {\np{color:black}\n}");

    for (const qualifier of ["layer(theme)", "supports(display: grid)"]) {
        await assert.rejects(() => processStyle({
            src: "/console/_assets/demo/pages/customer.js", context, css: `@import "./print.css" ${qualifier};`
        }), /Unsupported CSS @import qualifiers/);
    }

    setStylesheets(new Map([[`${moduleRoot}pages/loop.css`, '@import "./customer.js";']]), []);
    await assert.rejects(() => processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: '@import "./loop.css";'
    }), /Circular CSS @import detected/);
});

test("comments and ordinary strings stay intact while quoted and unquoted URLs are rewritten", async () => {
    setStylesheets(new Map(), []);
    const css = await processStyle({
        src: "/console/_assets/demo/pages/customer.js", context,
        css: '/* url(ignore.png) @import "ignore.css"; */\n.x::before{content:"url(ignore.png)";background:url( "./image.png" )}\n.y{background:url(image.png)}'
    });
    assert.equal(css, [
        '/* url(ignore.png) @import "ignore.css"; */',
        `.x::before{content:"url(ignore.png)";background:url( "${moduleRoot}pages/image.png" )}`,
        `.y{background:url(${moduleRoot}pages/image.png)}`
    ].join("\n"));
});

test("module and standard URL namespaces retain their CSS semantics", async () => {
    const requests = [];
    setStylesheets(new Map(), requests);
    const css = await processStyle({
        src: "/console/_assets/demo/pages/customer.js", context,
        css: [
            'a{background:url(/pages/images/compiled.png)}',
            'd{filter:url(#filter)}',
            'e{background:url(//cdn.test/icon.png)}',
            'f{background:url(https://cdn.test/icon.png)}',
            'g{background:url(data:image/png;base64,AAAA)}',
            'h{background:url(blob:https://example.test/id)}'
        ].join("\n")
    });

    assert.deepEqual(requests, []);
    assert.equal(css, [
        `a{background:url(${moduleRoot}pages/images/compiled.png)}`,
        "d{filter:url(#filter)}",
        "e{background:url(//cdn.test/icon.png)}",
        "f{background:url(https://cdn.test/icon.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n"));
});

test("CSS rejects configuration-only schemes in resource values, imports, and root stylesheet sources", async () => {
    const requests = [];
    setStylesheets(new Map(), requests);
    for (const css of [
        "a{background:url(url:./image.png)}",
        "a{background:url('url:./image.png')}",
        'a{background:url("URL:./image.png")}',
        '@import "url:./theme.css";',
        "@import url(url:./theme.css);",
        "@import url('URL:./theme.css');",
        "a{background:url(app:/image.png)}",
        "a{background:url('APP:/image.png')}",
        '@import "app:/theme.css";',
        "@import url(APP:/theme.css);"
    ]) {
        await assert.rejects(() => processStyle({
            src: "/console/_assets/demo/pages/customer.js", context, css
        }), /scheme is not supported in CSS resource references/);
    }
    await assert.rejects(() => processStyle({ src: "url:./site.css", context }),
        /'url:' scheme is not supported in CSS resource references/);
    await assert.rejects(() => processStyle({ src: "APP:./site.css", context }),
        /'app:' scheme is not supported in CSS resource references/);
    const literal = 'a{content:"url:./literal"}/* url:./comment */';
    assert.equal(await processStyle({ src: "/console/_assets/demo/pages/customer.js", context, css: literal }), literal);
    assert.deepEqual(requests, []);
});

test("custom CSS without modulePath uses declaring URL and normal origin-root semantics", async () => {
    const requests = [];
    setStylesheets(new Map([
        ["https://example.test/custom/theme.css", "theme{background:url(./theme.png)}"]
    ]), requests);
    const customContext = { appBasePath: "/console", resourceDefinition: {} };
    const css = await processStyle({
        src: "https://example.test/custom/card.js", context: customContext,
        css: '@import "./theme.css";\n.card{background:url(./card.png)}\n.root{background:url(/root.png)}'
    });
    assert.deepEqual(requests, ["https://example.test/custom/theme.css"]);
    assert.equal(css, [
        "theme{background:url(https://example.test/custom/theme.png)}",
        ".card{background:url(https://example.test/custom/card.png)}",
        ".root{background:url(https://example.test/root.png)}"
    ].join("\n"));
});

test("module and application traversal outside their roots is rejected", async () => {
    setStylesheets(new Map(), []);
    await assert.rejects(() => processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: "a{background:url(../../../outside.png)}"
    }), /escapes the module root/);
    await assert.rejects(() => processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: "a{background:url(/../../outside.png)}"
    }), /escapes the module root/);
});
