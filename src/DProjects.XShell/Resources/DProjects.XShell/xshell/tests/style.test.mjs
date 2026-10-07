import assert from "node:assert/strict";
import test from "node:test";
import { processStyle } from "../utils/style.js";

const moduleRoot = "https://example.test/console/_assets/demo/";
const moduleAssetsRoot = "https://cdn.test/modules/demo/";
const context = {
    appBasePath: "/console",
    resourceDefinition: { modulePath: "/_assets/demo", assetsUrl: moduleAssetsRoot }
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

test("module, app, physical and special URL namespaces retain their CSS semantics", async () => {
    const requests = [];
    setStylesheets(new Map([
        [`${moduleAssetsRoot}styles/theme.css`, "a{background:url('./icon.png')}"],
        ["https://example.test/console/styles/shared.css", "b{background:url('./shared.png')}\nroot{background:url(/root.png)}"]
    ]), requests);
    const css = await processStyle({
        src: "/console/_assets/demo/pages/customer.js", context,
        css: [
            '@import "url:../styles/theme.css";',
            '@import "app:/styles/shared.css";',
            'a{background:url(/pages/images/compiled.png)}',
            'b{background:url(app:/images/app.png)}',
            'c{background:url(url:./physical.png)}',
            'd{filter:url(#filter)}',
            'e{background:url(//cdn.test/icon.png)}',
            'f{background:url(https://cdn.test/icon.png)}',
            'g{background:url(data:image/png;base64,AAAA)}',
            'h{background:url(blob:https://example.test/id)}'
        ].join("\n")
    });

    assert.deepEqual(requests, [`${moduleAssetsRoot}styles/theme.css`, "https://example.test/console/styles/shared.css"]);
    assert.equal(css, [
        `a{background:url('${moduleRoot}styles/icon.png')}`,
        "b{background:url('https://example.test/console/styles/shared.png')}",
        "root{background:url(https://example.test/console/root.png)}",
        `a{background:url(${moduleRoot}pages/images/compiled.png)}`,
        "b{background:url(https://example.test/console/images/app.png)}",
        `c{background:url(${moduleAssetsRoot}pages/physical.png)}`,
        "d{filter:url(#filter)}",
        "e{background:url(//cdn.test/icon.png)}",
        "f{background:url(https://cdn.test/icon.png)}",
        "g{background:url(data:image/png;base64,AAAA)}",
        "h{background:url(blob:https://example.test/id)}"
    ].join("\n"));
});

test("module and application traversal outside their roots is rejected", async () => {
    setStylesheets(new Map(), []);
    await assert.rejects(() => processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: "a{background:url(../../../outside.png)}"
    }), /escapes the module root/);
    await assert.rejects(() => processStyle({
        src: "/console/_assets/demo/pages/customer.js", context, css: '@import "app:../../outside.css";'
    }), /escapes the application root/);
});
