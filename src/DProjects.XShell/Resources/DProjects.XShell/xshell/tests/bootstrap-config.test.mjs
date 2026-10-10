import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";

import Modules from "../modules.js";
import Resolver from "../resolver.js";

const rootUrl = "https://example.test/modules/app/module.jsonc";
const bootstrapPath = new URL("../bootstrap.js", import.meta.url);
const bootstrapSource = readFileSync(bootstrapPath, "utf8").replace(
    /\/\/ exec bootstrap[\s\S]*$/,
    `globalThis.__bootstrapTests = {
        parseJsonc,
        getLocalModule: (config, configUrl) => getLocalModule(JSON.parse(JSON.stringify(config)), configUrl),
        discover: (rootConfig, rootConfigUrl, configs, calls, environment = "Development") => discoverModuleConfigs(
            JSON.parse(JSON.stringify(rootConfig)),
            rootConfigUrl,
            async configUrl => {
                calls.push(configUrl);
                if (!Object.hasOwn(configs, configUrl)) throw new Error(\`Missing test config: \${configUrl}\`);
                return JSON.parse(JSON.stringify(configs[configUrl]));
            },
            "/_assets",
            environment
        ),
        mergeConfigs,
        loadConfig,
        loadFilesIndexes,
        fillResolverRules,
        initializeXShell,
        installServiceWorker,
        getAssetsPath,
        normalizeAssetsBase: normalizeAssetsBasePath,
        relativizePaths
    };`
);
const fetchedResources = new Map();
const fetchOverrides = new Map();
const directFetchCalls = [];
const metaValues = {
    "xshell:app.configPath": "/modules/app/module.jsonc",
    "xshell:app.params": "mode=host",
    "xshell:app.basePath": "/app",
    "xshell:xshell.environment": "",
    "xshell:xshell.temp.url": ""
};
const context = vm.createContext({
    URL,
    URLSearchParams,
    console: { log() {}, error() {} },
    document: {
        baseURI: "https://example.test/",
        currentScript: { src: "https://example.test/xshell/bootstrap.js" },
        head: {
            querySelector(selector) {
                const name = selector.match(/meta\[name="(.+)"\]/)?.[1];
                return name && Object.hasOwn(metaValues, name) ? { content: metaValues[name] } : null;
            }
        },
        location: { origin: "https://example.test" }
    },
    async fetch(url) {
        directFetchCalls.push(url);
        if (fetchOverrides.has(url)) return fetchOverrides.get(url)();
        const value = fetchedResources.get(url);
        return value === undefined ? {
            ok: false,
            status: 404,
            statusText: "Not Found",
            async text() { return ""; },
            async json() { throw new Error("No JSON response body."); }
        } : {
            ok: true,
            status: 200,
            statusText: "OK",
            async text() { return JSON.stringify(value); },
            async json() { return JSON.parse(JSON.stringify(value)); }
        };
    },
    window: { location: { origin: "https://example.test" } }
});
new vm.Script(bootstrapSource, { filename: bootstrapPath.pathname }).runInContext(context);
const api = context.__bootstrapTests;

test("browser JSONC accepts comments and trailing commas without changing string content", () => {
    const value = api.parseJsonc(`{
        // comment
        "a": 1,
        "array": [1, 2,],
        "url": "http://example.com/a,b",
        "block": "/* not a comment */",
        "line": "// not a comment",
        "ending": "value,}",
        "escaped": "quote \\" and slash \\\\ ",
    }`);
    assert.deepEqual(JSON.parse(JSON.stringify(value)), {
        a: 1, array: [1, 2], url: "http://example.com/a,b", block: "/* not a comment */",
        line: "// not a comment", ending: "value,}", escaped: 'quote " and slash \\ '
    });
});

test("generated Markdown Page rules preserve document URLs and cache classes by path", () => {
    const config = {
        app: { basePath: "/app", baseUrl: "https://example.test/app/" },
        modules: { "xshell-docs": { assetsPath: "/_assets/xshell-docs", files: [] }, sample: { assetsPath: "/_assets/sample", files: [] } },
        xshell: { resolver: {} }
    };
    api.fillResolverRules(config);
    const resolver = new Resolver({ config });
    for (const moduleId of ["xshell-docs", "sample"]) {
        const path = `/_assets/${moduleId}/pages/10-architecture/100-services.md`;
        const result = resolver.resolve(`page:${path}?mode=one#section`);
        assert.equal(result.path, path);
        assert.equal(result.url, "https://example.test/app" + path);
        assert.equal(result.definition.loader, "page-md");
        assert.equal(result.definition.cache, true);
        assert.equal(result.definition.cacheMode, "path");
        assert.equal(result.definition.cacheMode, config.xshell.resolver.page[`/_assets/${moduleId}/{path}.js`].cacheMode);
    }
});

test("generated style rules route module CSS through the standard cached style-css loader", () => {
    const config = {
        app: { basePath: "/app", baseUrl: "https://example.test/app/" },
        modules: { sample: { assetsPath: "/_assets/sample", files: [{ path: "/_assets/sample/styles/index.css" }] } },
        xshell: { resolver: {} }
    };
    api.fillResolverRules(config);

    const result = new Resolver({ config }).resolve("style:/_assets/sample/styles/index.css");
    assert.equal(result.path, "/_assets/sample/styles/index.css");
    assert.equal(result.url, "https://example.test/app/_assets/sample/styles/index.css");
    assert.equal(result.definition.loader, "style-css");
    assert.equal(result.definition.cache, true);
    assert.equal(result.definition.moduleId, "sample");
    assert.equal(result.definition.modulePath, "/_assets/sample");
});

const definition = (value, extra = {}) => ({ label: value, value, version: "1.0.0", ...extra });
const reference = configUrl => ({ configUrl });
const plain = value => JSON.parse(JSON.stringify(value));
async function discover(root, configs = {}, calls = []) {
    return api.discover(root, rootUrl, configs, calls);
}
function orderOf(graph) {
    return Array.from(graph.mergeOrder, node => node.id);
}

test("module discovery accepts canonical module ids", async () => {
    for (const moduleId of ["x", "app", "orders", "x-demo", "orders-v2", "xshell-docs"]) {
        await assert.doesNotReject(discover({ modules: { [moduleId]: definition(moduleId) } }));
    }
});

test("module discovery rejects malformed and reserved local module ids before path construction", async () => {
    for (const moduleId of ["", "XDemo", "123", "x_demo", "x.demo", "x/demo", "x demo", "-x-demo", "x-demo-", "x--demo", "café", "foo%20bar"]) {
        await assert.rejects(discover({ modules: { [moduleId]: definition(moduleId, { assetsUrl: "/not-used" }) } }),
            error => error.message === `Invalid module id '${moduleId}' in module configuration '${rootUrl}'.`);
    }

    await assert.rejects(discover({ modules: { xshell: definition("xshell", { assetsUrl: "/not-used" }) } }), /Module id 'xshell' is reserved/);
});

test("module discovery rejects malformed and reserved external reference ids before loading them", async () => {
    for (const moduleId of ["bad/id", "xshell"]) {
        const calls = [];
        await assert.rejects(discover({ modules: { app: definition("app"), [moduleId]: reference("./other/module.jsonc") } }, {}, calls),
            moduleId === "xshell" ? /Module id 'xshell' is reserved/ : /Invalid module id 'bad\/id'/);
        assert.deepEqual(calls, []);
    }
});

test("checked-in assetsBasePath resolves for root and subpath hosting", () => {
    const defaults = api.parseJsonc(readFileSync(new URL("../xshell.jsonc", import.meta.url), "utf8"));
    assert.equal(defaults.xshell.assetsBasePath, "app:/_assets");
    for (const [basePath, effectiveUrl] of [["", "https://example.test/_assets"], ["/myapp", "https://example.test/myapp/_assets"]]) {
        const isolated = vm.createContext({
            URL, URLSearchParams,
            document: {
                location: { origin: "https://example.test" },
                currentScript: { src: "https://example.test/xshell/bootstrap.js" },
                head: { querySelector(selector) { return { content: selector.includes("app.basePath") ? basePath : "" }; } }
            }
        });
        new vm.Script(bootstrapSource).runInContext(isolated);
        const base = isolated.__bootstrapTests.normalizeAssetsBase(defaults.xshell.assetsBasePath, "https://example.test/xshell/xshell.json");
        assert.equal(base, "/_assets");
        assert.equal(new URL(base.substring(1), `https://example.test${basePath}/`).href, effectiveUrl);
        const generated = api.getAssetsPath(base, "orders", "1.4.0", undefined, "Development");
        assert.equal(generated, "/_assets/orders/1.4.0.dev");
        assert.equal(new URL(`${generated.substring(1)}/pages/index.js`, `https://example.test${basePath}/`).pathname,
            `${basePath}/_assets/orders/1.4.0.dev/pages/index.js`);
    }
});

test("assetsBasePath rejects missing, application-root, and out-of-scope locations", () => {
    const configUrl = "https://example.test/xshell/xshell.json";
    assert.throws(() => api.normalizeAssetsBase(undefined, configUrl), /non-empty/);
    assert.throws(() => api.normalizeAssetsBase("app:/", configUrl), /namespace below/);
    assert.throws(() => api.normalizeAssetsBase("https://example.test/elsewhere/assets", configUrl), /within the application base/);
    assert.throws(() => api.normalizeAssetsBase("/_assets", configUrl), /must use app: or an absolute HTTP\(S\) URL/);
    assert.throws(() => api.normalizeAssetsBase("url:./assets", configUrl), /must use app: or an absolute HTTP\(S\) URL/);
});

test("development and published generations use descriptor version and package hash", async () => {
    assert.equal(api.getAssetsPath("/_assets", "orders", "1.4.0", undefined, "Development"), "/_assets/orders/1.4.0.dev");
    assert.equal(api.getAssetsPath("/_assets", "xshell", "0.9.0", undefined, "development"), "/_assets/xshell/0.9.0.dev");
    assert.equal(api.getAssetsPath("/_assets", "orders", "1.4.0", "ignored", "DEVELOPMENT"), "/_assets/orders/1.4.0.dev");
    assert.equal(api.getAssetsPath("/_assets", "orders", "1.4.0", "a82c31f943e01abc", "Production"), "/_assets/orders/1.4.0.a82c31f943e01abc");
    assert.equal(api.getAssetsPath("/_assets", "xshell", "0.9.0", "31d04ab8e220a581", "Staging"), "/_assets/xshell/0.9.0.31d04ab8e220a581");
    assert.throws(() => api.getAssetsPath("/_assets", "xshell", "0.9.0", "", "Production"), /Published module 'xshell'.*hash/);
    assert.throws(() => api.getAssetsPath("/_assets", "orders", "1.4.0", "  ", "Production"), /Published module 'orders'.*hash/);

    const root = { modules: { orders: definition("orders", { version: "1.4.0", hash: "a82c31f943e01abc", controller: "/module.js" }) } };
    const development = await api.discover(root, rootUrl, {}, [], "Development");
    const published = await api.discover(root, rootUrl, {}, [], "Production");
    assert.equal(development.rootNode.assetsPath, "/_assets/orders/1.4.0.dev");
    assert.equal(development.rootNode.config.modules.orders.controller, "/_assets/orders/1.4.0.dev/module.js");
    assert.equal(published.rootNode.assetsPath, "/_assets/orders/1.4.0.a82c31f943e01abc");
    assert.equal(published.rootNode.config.modules.orders.controller, "/_assets/orders/1.4.0.a82c31f943e01abc/module.js");
    const dependencyUrl = "https://example.test/modules/app/dependency/module.jsonc";
    const calls = [];
    const withDependency = await api.discover({ modules: { ...root.modules, dependency: reference("./dependency/module.jsonc") } }, rootUrl,
        { [dependencyUrl]: { modules: { dependency: definition("dependency", { hash: "dependency-hash" }) } } }, calls, "Production");
    assert.equal(withDependency.rootNode.references[0].configUrl,
        "/_assets/orders/1.4.0.a82c31f943e01abc/dependency/module.jsonc");
    assert.equal(withDependency.rootNode.references[0].loadUrl, dependencyUrl);
    assert.deepEqual(calls, [dependencyUrl]);
    await assert.rejects(api.discover({ modules: { orders: definition("orders", { version: "1.4.0" }) } }, rootUrl, {}, [], "Production"),
        /Published module 'orders'.*hash/);
});

test("module URLs normalize against the declaring logical file and keep explicit escapes distinct", () => {
    const logicalFile = "/pages/orders/details.jsonc";
    const physicalFile = "https://cdn.example.test/orders/pages/details.jsonc";
    const normalize = value => api.relativizePaths(value, "/_assets/x", logicalFile, physicalFile);

    assert.equal(normalize("/icons/edit.svg"), "/_assets/x/icons/edit.svg");
    assert.equal(normalize("./edit.js"), "/_assets/x/pages/orders/edit.js");
    assert.equal(normalize("../shared.js"), "/_assets/x/pages/shared.js");
    assert.equal(normalize("url:./edit.js"), "https://cdn.example.test/orders/pages/edit.js");
    assert.equal(normalize("url:../shared.js"), "https://cdn.example.test/orders/shared.js");
    assert.notEqual(normalize("./edit.js"), normalize("url:./edit.js"));
    assert.equal(normalize("app:foo"), "https://example.test/app/foo");
    assert.equal(normalize("app:/foo"), "https://example.test/app/foo");
    assert.equal(normalize("app:/state-engines/{name}.js"), "https://example.test/app/state-engines/{name}.js");
    assert.equal(normalize("url:./{name}.js"), "https://cdn.example.test/orders/pages/{name}.js");
    assert.equal(normalize("/%7Bname%7D.js"), "/_assets/x/%7Bname%7D.js");
    for (const absolute of ["https://cdn.example.test/file.js", "data:text/javascript,export%20default%201", "blob:https://example.test/1234"]) {
        assert.equal(normalize(absolute), absolute);
    }
    assert.equal(normalize("foo/bar.js"), "foo/bar.js");
});

test("bootstrap loads the authored XShell JSONC source through the canonical runtime URL", async () => {
    const xshellUrl = "https://example.test/xshell/xshell.json";
    fetchedResources.set(xshellUrl, api.parseJsonc(readFileSync(new URL("../xshell.jsonc", import.meta.url), "utf8")));
    fetchedResources.set(rootUrl, { modules: { app: definition("app") } });

    const config = plain(await api.loadConfig());
    const resolver = new Resolver({ config });
    assert.equal(config.xshell.configUrl, xshellUrl);
    assert.equal(config.xshell.resolver["state-engine"]["{name}"].src, "/_assets/xshell/0.9.0.dev/state-engines/{name}.js");
    assert.equal(resolver.resolve("state-engine:proxy").url, "https://example.test/app/_assets/xshell/0.9.0.dev/state-engines/proxy.js");
    assert.equal(resolver.resolve("render-engine:x").url, "https://example.test/app/_assets/xshell/0.9.0.dev/render-engines/x.js");
    assert.equal(resolver.resolve("schema:config.json").url, "https://example.test/app/_assets/xshell/0.9.0.dev/schemas/config.json");
});

test("module URL traversal fails after logical normalization and reports the declaring document", () => {
    const physicalFile = "https://cdn.example.test/orders/module.jsonc";
    for (const value of ["../foo.js", "../../foo.js", "./../foo.js", "a/../../foo.js", "/a/../../foo.js", "%2e%2e/foo.js"]) {
        assert.throws(() => api.relativizePaths(value, "/_assets/x", "/module.jsonc", physicalFile), error =>
            error.message.includes(value) && error.message.includes(physicalFile) && error.message.includes("escapes the module root"));
    }
});

test("module discovery keeps logical configUrl separate from its pre-Service-Worker physical fetch URL", async () => {
    const physicalUrl = "https://example.test/modules/app/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference("./x/module.jsonc") } };
    const calls = [];
    const graph = await discover(root, { [physicalUrl]: { modules: { x: definition("x") } } }, calls);

    assert.deepEqual(calls, [physicalUrl]);
    assert.equal(graph.rootNode.references[0].configUrl, "/_assets/app/1.0.0.dev/x/module.jsonc");
    assert.equal(graph.rootNode.config.modules.x.configUrl, "/_assets/app/1.0.0.dev/x/module.jsonc");
    assert.equal(graph.nodesById.get("x").config.modules.x.configUrl, physicalUrl);

    const siblingUrl = "https://example.test/modules/x/module.jsonc";
    const physical = await discover({ modules: { app: definition("app"), x: reference("url:../x/module.jsonc") } },
        { [siblingUrl]: { modules: { x: definition("x") } } });
    assert.equal(physical.rootNode.references[0].configUrl, siblingUrl);
    await assert.rejects(() => discover({ modules: { app: definition("app"), x: reference("../x/module.jsonc") } }), error =>
        error.message.includes("../x/module.jsonc") && error.message.includes(rootUrl) && error.message.includes("escapes the module root"));
    await assert.rejects(() => discover({ modules: { app: definition("app"), x: reference("module.jsonc") } }), /unsupported configUrl/);
});

test("module configuration URLs use the file's logical path when assetsUrl names its source root", async () => {
    const sourceUrl = "https://cdn.example.test/orders/pages/orders/details.jsonc";
    const config = { modules: { orders: definition("orders", { assetsUrl: "url:../../", controller: "./edit.js", routes: { "/shared": "../shared.js" } }) } };
    const graph = await api.discover(config, sourceUrl, {}, []);

    assert.equal(graph.rootNode.config.modules.orders.assetsUrl, "https://cdn.example.test/orders/");
    assert.equal(graph.rootNode.config.modules.orders.controller, "/_assets/orders/1.0.0.dev/pages/orders/edit.js");
    assert.equal(graph.rootNode.config.modules.orders.routes["/shared"], "/_assets/orders/1.0.0.dev/pages/shared.js");
});

test("non-URL configuration text and area prefixes keep their authored values", async () => {
    const root = {
        modules: { app: definition("app", { params: { note: "/literal", hint: "../literal" }, routes: { "/home": "/pages/index.js" } }) },
        xshell: { areas: { definitions: { main: { prefix: "/public", label: "/literal" } } } }
    };
    const graph = await discover(root);
    assert.deepEqual(plain(graph.rootNode.config.modules.app.params), { note: "/literal", hint: "../literal" });
    assert.equal(graph.rootNode.config.xshell.areas.definitions.main.prefix, "/public");
    assert.equal(graph.rootNode.config.xshell.areas.definitions.main.label, "/literal");
    assert.equal(graph.rootNode.config.modules.app.routes["/home"], "/_assets/app/1.0.0.dev/pages/index.js");
});

test("menu contributions normalize by structure regardless of menu name", async () => {
    const root = { modules: { x: definition("x", { menus: {
        navigation: "/pages/navigation",
        tools: "/pages/tools",
        arbitraryMenu123: "/pages/custom",
        registered: "customer-pages",
        reports: [{
            label: "Reports", path: "/reports", href: "/pages/report.js", tooltip: "/literal-tooltip",
            children: [{ label: "Detail", path: "/reports/detail", href: "./pages/detail.js", class: "/literal-class" }]
        }]
    }, params: { metadata: { path: "/must-remain-literal" }, sample: { href: "/literal", url: "../literal" } } }) } };
    const graph = await discover(root);
    const module = plain(graph.rootNode.config.modules.x);

    assert.equal(module.menus.navigation, "/_assets/x/1.0.0.dev/pages/navigation");
    assert.equal(module.menus.tools, "/_assets/x/1.0.0.dev/pages/tools");
    assert.equal(module.menus.arbitraryMenu123, "/_assets/x/1.0.0.dev/pages/custom");
    assert.equal(module.menus.registered, "customer-pages");
    assert.equal(module.menus.reports[0].path, "/reports");
    assert.equal(module.menus.reports[0].href, "/_assets/x/1.0.0.dev/pages/report.js");
    assert.equal(module.menus.reports[0].tooltip, "/literal-tooltip");
    assert.equal(module.menus.reports[0].children[0].path, "/reports/detail");
    assert.equal(module.menus.reports[0].children[0].href, "/_assets/x/1.0.0.dev/pages/detail.js");
    assert.equal(module.menus.reports[0].children[0].class, "/literal-class");
    assert.deepEqual(module.params, { metadata: { path: "/must-remain-literal" }, sample: { href: "/literal", url: "../literal" } });
});

test("resolver, service, UI, and Area resources normalize within their owning structures", async () => {
    const root = {
        modules: { x: definition("x") },
        xshell: {
            resolver: { anyType: { anyRule: { src: "/resources/{name}.js", loader: "/literal-loader" } } },
            services: { foo: { implementation: "/services/foo.js", contract: "/literal-contract" }, bar: { implementation: "./services/bar.js" } },
            ui: { component: { lazy: "x-lazy", error: "/components/error.js", unrelated: "/literal" }, layout: { main: "x-layout-main" },
                dialog: { confirm: "url:./dialog.js" }, unrelated: { label: "/literal" } },
            areas: { definitions: { main: { prefix: "/public", icon: "/icons/main.svg", label: "/literal-label" } } }
        }
    };
    const xshell = plain((await discover(root)).rootNode.config.xshell);

    assert.equal(xshell.resolver.anyType.anyRule.src, "/_assets/x/1.0.0.dev/resources/{name}.js");
    assert.equal(xshell.resolver.anyType.anyRule.loader, "/literal-loader");
    assert.equal(xshell.services.foo.implementation, "/_assets/x/1.0.0.dev/services/foo.js");
    assert.equal(xshell.services.bar.implementation, "/_assets/x/1.0.0.dev/services/bar.js");
    assert.equal(xshell.services.foo.contract, "/literal-contract");
    assert.equal(xshell.ui.component.lazy, "x-lazy");
    assert.equal(xshell.ui.component.error, "/_assets/x/1.0.0.dev/components/error.js");
    assert.equal(xshell.ui.component.unrelated, "/literal");
    assert.equal(xshell.ui.layout.main, "x-layout-main");
    assert.equal(xshell.ui.dialog.confirm, "https://example.test/modules/app/dialog.js");
    assert.equal(xshell.ui.unrelated.label, "/literal");
    assert.equal(xshell.areas.definitions.main.prefix, "/public");
    assert.equal(xshell.areas.definitions.main.icon, "/_assets/x/1.0.0.dev/icons/main.svg");
    assert.equal(xshell.areas.definitions.main.label, "/literal-label");
});

test("assetsUrl requires an explicit physical or application URL", async () => {
    await assert.rejects(() => discover({ modules: { x: definition("x", { assetsUrl: "/physical-looking-path" }) } }), /assetsUrl must use app:, url:, or an absolute URL/);
});

test("Service Worker rules consume normalized framework and module paths", async () => {
    let rules;
    context.navigator = { serviceWorker: {
        controller: {}, ready: Promise.resolve(),
        async register(url, options) {
            assert.equal(url, "/app/sw.js");
            assert.equal(options.scope, "/app/");
            return { active: { postMessage(message, ports) { rules = message.payload.rules; ports[0].reply({ type: "ready" }); } } };
        }
    } };
    context.MessageChannel = class {
        constructor() {
            this.port1 = { onmessage: null, close() {} };
            this.port2 = { reply: data => this.port1.onmessage({ data }) };
        }
    };
    context.setTimeout = () => 0;
    const config = {
        xshell: { assetsBasePath: "/runtime", assetsPath: "/runtime/xshell/1.dev", assetsUrl: "https://example.test/framework/", configUrl: "https://example.test/xshell/xshell.json", version: "1" },
        modules: { x: { assetsPath: "/runtime/x/2.dev", assetsUrl: "https://example.test/modules/x/", configUrl: rootUrl, version: "2" } }
    };

    assert.equal(await api.installServiceWorker(config), true);
    assert.deepEqual(plain(rules.map(({ src, dst }) => ({ src, dst }))), [
        { src: "https://example.test/app/runtime/xshell/1.dev", dst: "https://example.test/framework/" },
        { src: "https://example.test/app/runtime/x/2.dev", dst: "https://example.test/modules/x/" }
    ]);
});

test("loadFilesIndexes concurrently loads module and XShell inventories through the virtual namespace", async () => {
    const xUrl = "https://example.test/app/_assets/x/1.0.0.dev/module.files.json";
    const reportsUrl = "https://example.test/app/_assets/reports/2.0.0.abc123/module.files.json";
    const xshellUrl = "https://example.test/app/_assets/xshell/0.9.0.dev/module.files.json";
    const pending = new Map();
    const response = value => ({
        ok: true,
        status: 200,
        statusText: "OK",
        async json() { return JSON.parse(JSON.stringify(value)); }
    });
    for (const url of [xUrl, reportsUrl, xshellUrl]) {
        fetchOverrides.set(url, () => new Promise(resolve => pending.set(url, resolve)));
    }
    const config = {
        modules: { x: { assetsPath: "/_assets/x/1.0.0.dev" }, reports: { assetsPath: "/_assets/reports/2.0.0.abc123" } },
        xshell: { assetsBasePath: "/_assets", assetsPath: "/_assets/xshell/0.9.0.dev" }
    };

    const loading = api.loadFilesIndexes(config);
    await Promise.resolve();

    assert.deepEqual([...pending.keys()], [xUrl, reportsUrl, xshellUrl]);
    pending.get(xUrl)(response([{ path: "/components/x-button.js", size: 1234, hash: "x-hash" }]));
    pending.get(reportsUrl)(response([{ path: "/pages/home.js", size: 25, hash: "reports-hash" }]));
    pending.get(xshellUrl)(response([{ path: "/xshell.js", size: 5678, hash: "xshell-hash" }]));
    await loading;

    assert.deepEqual(plain(config.modules.x.files), [{ path: "/_assets/x/1.0.0.dev/components/x-button.js", size: 1234, hash: "x-hash" }]);
    assert.deepEqual(plain(config.modules.reports.files), [{ path: "/_assets/reports/2.0.0.abc123/pages/home.js", size: 25, hash: "reports-hash" }]);
    assert.deepEqual(plain(config.xshell.files), [{ path: "/_assets/xshell/0.9.0.dev/xshell.js", size: 5678, hash: "xshell-hash" }]);
    for (const url of [xUrl, reportsUrl, xshellUrl]) fetchOverrides.delete(url);
});

test("loadFilesIndexes reports the inventory id, URL, and HTTP failure", async () => {
    const inventoryUrl = "https://example.test/app/_assets/x/module.files.json";
    const xshellInventoryUrl = "https://example.test/app/_assets/xshell/module.files.json";
    fetchOverrides.set(inventoryUrl, async () => ({ ok: false, status: 503, statusText: "Service Unavailable" }));
    fetchedResources.set(xshellInventoryUrl, []);
    const config = { modules: { x: { assetsPath: "/_assets/x" } }, xshell: { assetsBasePath: "/_assets", assetsPath: "/_assets/xshell" } };

    await assert.rejects(
        () => api.loadFilesIndexes(config),
        error => error.message.includes("'x'") && error.message.includes(inventoryUrl) && error.message.includes("503 Service Unavailable")
    );
    fetchOverrides.delete(inventoryUrl);
    fetchedResources.delete(xshellInventoryUrl);
});

test("loadFilesIndexes fails clearly when the XShell inventory is missing", async () => {
    const inventoryUrl = "https://example.test/app/_assets/xshell/module.files.json";
    const config = { modules: {}, xshell: { assetsBasePath: "/_assets", assetsPath: "/_assets/xshell" } };

    await assert.rejects(
        () => api.loadFilesIndexes(config),
        error => error.message.includes("'xshell'") && error.message.includes(inventoryUrl) && error.message.includes("404 Not Found")
    );
});

test("fillResolverRules discovers only JSON files inside the contracts directory", () => {
    const config = {
        modules: {
            x: {
                assetsPath: "/_assets/x",
                files: [
                    { path: "/_assets/x/contracts/toast.json" },
                    { path: "/_assets/x/contracts/readme.txt" },
                    { path: "/_assets/x/contracts-old/legacy.json" }
                ]
            }
        },
        xshell: { resolver: {} }
    };

    api.fillResolverRules(config);

    assert.deepEqual(plain(config.xshell.resolver.contract), {
        toast: {
            src: "/_assets/x/contracts/toast.json",
            loader: "object-json",
            cache: true,
            moduleId: "x",
            modulePath: "/_assets/x"
        }
    });
});

test("fillResolverRules rejects duplicate global contract IDs with both declarations", () => {
    const config = {
        modules: {
            x: { assetsPath: "/_assets/x", files: [{ path: "/_assets/x/contracts/identity.json" }] },
            auth: { assetsPath: "/_assets/auth", files: [{ path: "/_assets/auth/contracts/identity.json" }] }
        },
        xshell: { resolver: {} }
    };

    assert.throws(
        () => api.fillResolverRules(config),
        error => error.message.includes("Duplicate contract 'identity'") && error.message.includes("module 'x'") &&
            error.message.includes("/_assets/x/contracts/identity.json") && error.message.includes("module 'auth'") &&
            error.message.includes("/_assets/auth/contracts/identity.json")
    );
});

test("effective inventories exist before validation and configuration is frozen before init", async () => {
    const events = [];
    const config = {
        modules: { x: { assetsPath: "/_assets/x", files: [{ path: "/_assets/x/file.js", size: 1, hash: "x" }] } },
        xshell: {
            assetsPath: "/_assets/xshell",
            files: [{ path: "/_assets/xshell/xshell.js", size: 2, hash: "xshell" }],
            resolver: { module: { xshell: { src: "/_assets/xshell/xshell.js" } } }
        }
    };
    const runtime = {
        async validateConfig(value) {
            events.push("validate");
            assert.equal(value.modules.x.assetsPath, "/_assets/x");
            assert.equal(value.xshell.assetsPath, "/_assets/xshell");
            assert.equal(value.modules.x.files[0].path, "/_assets/x/file.js");
            assert.equal(value.xshell.files[0].path, "/_assets/xshell/xshell.js");
            assert.equal(Object.isFrozen(value), false);
        },
        async init(value) {
            events.push("init");
            assert.equal(value.modules.x.assetsPath, "/_assets/x");
            assert.equal(value.xshell.assetsPath, "/_assets/xshell");
            assert.equal(Object.isFrozen(value), true);
            assert.equal(Object.isFrozen(value.modules.x.files[0]), true);
        }
    };

    await api.initializeXShell(config, async url => {
        events.push(`import:${url}`);
        return { default: runtime };
    });

    assert.deepEqual(events, ["import:https://example.test/app/_assets/xshell/xshell.js", "validate", "init"]);
});

test("initializeXShell imports an absolute XShell module URL unchanged", async () => {
    const config = { xshell: { resolver: { module: { xshell: { src: "https://cdn.example.test/xshell.js" } } } } };
    let importedUrl;
    await api.initializeXShell(config, async url => {
        importedUrl = url;
        return { default: { async validateConfig() {}, async init() {} } };
    });
    assert.equal(importedUrl, "https://cdn.example.test/xshell.js");
});

test("loadConfig keeps xshellConfig as the base and applies root configuration last", async () => {
    directFetchCalls.length = 0;
    const xshellUrl = "https://example.test/xshell/xshell.json";
    fetchedResources.set(xshellUrl, {
        app: { source: "xshell" },
        modules: {},
        xshell: { assetsBasePath: "app:/_assets", environment: "Production", version: "0.9.0", hash: "31d04ab8e220a581", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, {
        app: { source: "root" },
        modules: { app: definition("app", { hash: "a82c31f943e01abc" }) }
    });
    const config = plain(await api.loadConfig());

    assert.equal(config.app.source, "root");
    assert.equal(config.app.basePath, "/app");
    assert.deepEqual(config.app.params, { mode: "host" });
    assert.deepEqual(config.modules.app.params, { mode: "host" });
    assert.equal(config.modules.app.assetsPath, "/_assets/app/1.0.0.a82c31f943e01abc");
    assert.equal(config.xshell.configUrl, xshellUrl);
    assert.equal(config.xshell.assetsBasePath, "/_assets");
    assert.equal(config.xshell.assetsPath, "/_assets/xshell/0.9.0.31d04ab8e220a581");
    config.modules.app.files = [];
    api.fillResolverRules(config);
    assert.equal(config.xshell.resolver.page["/_assets/app/1.0.0.a82c31f943e01abc/{path}.js"].modulePath,
        "/_assets/app/1.0.0.a82c31f943e01abc");
    assert.deepEqual(directFetchCalls, [xshellUrl, rootUrl]);
});

test("loadConfig derives every assetsPath from a custom assetsBasePath", async () => {
    const xshellUrl = "https://example.test/xshell/xshell.json";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    fetchedResources.set(xshellUrl, {
        app: {},
        modules: {},
        xshell: { assetsBasePath: "app:/runtime", assetsPath: "/authored-xshell", environment: "Production", version: "0.9.0", hash: "31d04ab8e220a581", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, {
        modules: { app: definition("app", { assetsPath: "/authored-app", hash: "app-hash" }), x: reference(xUrl) }
    });
    fetchedResources.set(xUrl, { modules: { x: definition("x", { assetsPath: "/authored-x", hash: "x-hash" }) } });

    const config = plain(await api.loadConfig());

    assert.equal(config.modules.app.assetsPath, "/runtime/app/1.0.0.app-hash");
    assert.equal(config.modules.x.assetsPath, "/runtime/x/1.0.0.x-hash");
    assert.deepEqual(config.modules.app.contract, { events: {} });
    assert.deepEqual(config.modules.x.contract, { events: {} });
    assert.equal(config.xshell.assetsBasePath, "/runtime");
    assert.equal(config.xshell.assetsPath, "/runtime/xshell/0.9.0.31d04ab8e220a581");
});

test("root assetsBasePath override is normalized before module discovery", async () => {
    const xshellUrl = "https://example.test/xshell/xshell.json";
    fetchedResources.set(xshellUrl, {
        app: {}, modules: {},
        xshell: { assetsBasePath: "app:/_assets", environment: "Development", version: "0.9.0", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, {
        modules: { app: definition("app") },
        xshell: { assetsBasePath: "app:/custom-assets" }
    });

    const config = plain(await api.loadConfig());
    assert.equal(config.xshell.assetsBasePath, "/custom-assets");
    assert.equal(config.xshell.assetsPath, "/custom-assets/xshell/0.9.0.dev");
    assert.equal(config.modules.app.assetsPath, "/custom-assets/app/1.0.0.dev");
    assert.equal(config.xshell.assetsUrl, "https://example.test/xshell/");
});

test("loadConfig resolves XShell assetsUrl from the application base", async () => {
    fetchedResources.set("https://example.test/xshell/xshell.json", {
        app: {}, modules: {},
        xshell: { assetsBasePath: "app:/_assets", environment: "Development", version: "0.9.0", assetsUrl: "app:/framework/", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, { modules: { app: definition("app") } });

    const config = plain(await api.loadConfig());

    assert.equal(config.xshell.assetsUrl, "https://example.test/app/framework/");
    assert.equal(config.xshell.assetsBasePath, "/_assets");
    assert.equal(config.xshell.assetsPath, "/_assets/xshell/0.9.0.dev");
});

test("host Development override selects mutable generations without package hashes", async () => {
    fetchedResources.set("https://example.test/xshell/xshell.json", {
        app: {}, modules: {},
        xshell: { assetsBasePath: "app:/_assets", environment: "Production", version: "0.9.0", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, { modules: { app: definition("app") } });
    const hostedDocument = {
        ...context.document,
        head: { querySelector: selector => selector === 'meta[name="xshell:xshell.environment"]' ? { content: "dEvElOpMeNt" } : context.document.head.querySelector(selector) }
    };
    const hostedContext = vm.createContext({ ...context, document: hostedDocument });
    new vm.Script(bootstrapSource, { filename: bootstrapPath.pathname }).runInContext(hostedContext);
    const config = plain(await hostedContext.__bootstrapTests.loadConfig());
    assert.equal(config.xshell.environment, "dEvElOpMeNt");
    assert.equal(config.xshell.assetsPath, "/_assets/xshell/0.9.0.dev");
    assert.equal(config.modules.app.assetsPath, "/_assets/app/1.0.0.dev");
});

test("non-Development XShell without a package hash fails clearly", async () => {
    fetchedResources.set("https://example.test/xshell/xshell.json", {
        app: {}, modules: {},
        xshell: { assetsBasePath: "app:/_assets", environment: "Production", version: "0.9.0", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, { modules: { app: definition("app", { hash: "app-hash" }) } });
    await assert.rejects(api.loadConfig(), /Published module 'xshell'.*hash/);
});

test("root config accepts exactly one local module regardless of module key order", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { x: reference("url:../x/module.jsonc"), app: definition("app") } };
    const graph = await discover(root, { [xUrl]: { modules: { x: definition("x") } } });

    assert.equal(graph.rootNode.id, "app");
    assert.deepEqual(orderOf(graph), ["x", "app"]);
    assert.equal(graph.rootNode.references[0].configUrl, xUrl);
    assert.equal(graph.rootNode.config.modules.app.assetsUrl, "https://example.test/modules/app/");    
});

test("app URLs use the fixed host application base across configuration documents and navigation", async () => {
    const nestedDocument = {
        ...context.document,
        baseURI: "https://example.test/myapp/first/route#one",
        head: { querySelector: selector => selector === 'meta[name="xshell:app.basePath"]' ? { content: "/myapp" } : context.document.head.querySelector(selector) },
        location: { origin: "https://example.test", pathname: "/myapp/first/route", hash: "#one" }
    };
    const nestedContext = vm.createContext({ ...context, document: nestedDocument });
    new vm.Script(bootstrapSource, { filename: bootstrapPath.pathname }).runInContext(nestedContext);
    const nestedApi = nestedContext.__bootstrapTests;
    const ownerUrl = "https://example.test/config/root/module.jsonc";
    const xUrl = "https://example.test/myapp/modules/x/module.jsonc";
    const yUrl = "https://example.test/myapp/modules/y/module.jsonc";
    const sourceUrl = "https://example.test/config/source/module.jsonc";
    const root = { modules: {
        app: definition("app", { assetsUrl: "app:foo" }),
        x: reference("app:modules/x/module.jsonc"),
        source: reference("url:../source/module.jsonc")
    } };
    const configs = {
        [xUrl]: { modules: { x: definition("x", { assetsUrl: "app:/foo" }), y: reference("app:/modules/y/module.jsonc") } },
        [yUrl]: { modules: { y: definition("y", { assetsUrl: "https://cdn.example.test/y/" }) } },
        [sourceUrl]: { modules: { source: definition("source", { assetsUrl: "url:./assets/" }) } }
    };
    const check = async () => {
        const graph = await nestedApi.discover(root, ownerUrl, configs, []);
        assert.equal(graph.rootNode.config.modules.app.assetsUrl, "https://example.test/myapp/foo");
        assert.equal(graph.nodesById.get("x").config.modules.x.assetsUrl, "https://example.test/myapp/foo");
        assert.equal(graph.nodesById.get("y").config.modules.y.assetsUrl, "https://cdn.example.test/y/");
        assert.equal(graph.nodesById.get("source").config.modules.source.assetsUrl, "https://example.test/config/source/assets/");
        assert.deepEqual(Array.from(graph.rootNode.references, reference => reference.configUrl), [xUrl, sourceUrl]);
        assert.equal(graph.nodesById.get("x").references[0].configUrl, yUrl);
    };

    await check();
    nestedDocument.baseURI = "https://example.test/myapp/other/route#two";
    nestedDocument.location.pathname = "/myapp/other/route";
    nestedDocument.location.hash = "#two";
    await check();
});

test("dependency config accepts exactly one local module regardless of module key order", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl) } };
    const configs = {
        [aUrl]: { modules: { x: reference(xUrl), a: definition("a") } },
        [xUrl]: { modules: { x: definition("x") } }
    };
    const graph = await discover(root, configs);

    assert.deepEqual(orderOf(graph), ["x", "a", "app"]);
});

test("zero local module definitions are rejected", async () => {
    const root = { modules: { x: reference("https://example.test/x.jsonc"), y: reference("https://example.test/y.jsonc") } };

    await assert.rejects(() => discover(root), /exactly one local module definition, but found 0/);
});

test("multiple local module definitions are rejected", async () => {
    const root = { modules: { a: definition("a"), b: definition("b") } };

    await assert.rejects(() => discover(root), /exactly one local module definition, but found 2: 'a', 'b'/);
});

test("a dependency with zero local module definitions is rejected", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(xUrl) } };
    const configs = { [xUrl]: { modules: { y: reference("https://example.test/modules/y/module.jsonc") } } };

    await assert.rejects(() => discover(root, configs), /exactly one local module definition, but found 0/);
});

test("a dependency with multiple local module definitions is rejected", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(xUrl) } };
    const configs = { [xUrl]: { modules: { x: definition("x"), y: definition("y") } } };

    await assert.rejects(() => discover(root, configs), /exactly one local module definition, but found 2: 'x', 'y'/);
});

test("a reference must match the loaded local module identity", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(xUrl) } };
    const configs = { [xUrl]: { modules: { y: definition("y") } } };

    await assert.rejects(() => discover(root, configs), /Module reference 'x'.*defines local module 'y'/);
});

test("a shared dependency with the same URL is fetched and registered once", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl), b: reference(bUrl) } };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: reference(xUrl) } },
        [bUrl]: { modules: { b: definition("b"), x: reference(xUrl) } },
        [xUrl]: { modules: { x: definition("x") } }
    };
    const calls = [];
    const graph = await discover(root, configs, calls);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.equal(calls.filter(url => url === xUrl).length, 1);
    assert.deepEqual(orderOf(graph), ["x", "a", "b", "app"]);
    assert.deepEqual(Object.keys(effective.modules).sort(), ["a", "app", "b", "x"]);

    // verify the canonical effective map creates one runtime record
    for (const module of Object.values(effective.modules)) module.files = [];
    const modules = new Modules({
        bus: {},
        config: { xshell: { assetsBasePath: "/_assets" }, modules: effective.modules },
        loader: { load() { throw new Error("No resources should be loaded by this fixture."); } },
        resolver: {},
        document: { adoptedStyleSheets: [] },
        services: {}
    });
    await modules.init();
    assert.equal(modules.registry.filter(module => module.id === "x").length, 1);
});

test("conflicting URLs for the same id discovered in one pass are rejected", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const x1Url = "https://example.test/modules/x-v1/module.jsonc";
    const x2Url = "https://example.test/modules/x-v2/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl), b: reference(bUrl) } };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: reference(x1Url) } },
        [bUrl]: { modules: { b: definition("b"), x: reference(x2Url) } }
    };
    const calls = [];

    await assert.rejects(() => discover(root, configs, calls), /Module 'x' is referenced with conflicting configUrl values/);
    assert.deepEqual(calls, [aUrl, bUrl]);
});

test("a conflicting URL discovered after the module was loaded is rejected", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const cUrl = "https://example.test/modules/c/module.jsonc";
    const x1Url = "https://example.test/modules/x-v1/module.jsonc";
    const x2Url = "https://example.test/modules/x-v2/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl), b: reference(bUrl) } };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: reference(x1Url) } },
        [bUrl]: { modules: { b: definition("b"), c: reference(cUrl) } },
        [cUrl]: { modules: { c: definition("c"), x: reference(x2Url) } },
        [x1Url]: { modules: { x: definition("x") } }
    };
    const calls = [];

    await assert.rejects(() => discover(root, configs, calls), /Module 'x' is referenced with conflicting configUrl values/);
    assert.deepEqual(calls, [aUrl, bUrl, x1Url, cUrl]);
});

test("transitive dependencies load and merge before their dependents", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { trace: ["root"], modules: { app: definition("app"), a: reference(aUrl) } };
    const configs = {
        [aUrl]: { trace: ["a"], modules: { a: definition("a"), x: reference(xUrl) } },
        [xUrl]: { trace: ["x"], modules: { x: definition("x") } }
    };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.deepEqual(orderOf(graph), ["x", "a", "app"]);
    assert.deepEqual(effective.trace, ["x", "a", "root"]);
});

test("child references retain params and arbitrary configuration contributions", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl) } };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: { configUrl: xUrl, params: { mode: "compact" }, someFeature: true } } },
        [xUrl]: { modules: { x: definition("x", { params: { mode: "normal", retained: true }, someFeature: false }) } }
    };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.deepEqual(orderOf(graph), ["x", "a", "app"]);
    assert.deepEqual(effective.modules.x.params, { mode: "compact", retained: true });
    assert.equal(effective.modules.x.someFeature, true);
});

test("references reject assetsUrl but require configUrl", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl) } };
    const configs = { [aUrl]: { modules: { a: definition("a"), x: { configUrl: xUrl, assetsUrl: "url:./assets" } } } };

    await assert.rejects(() => discover(root, configs), /cannot override assetsUrl/);
    await assert.rejects(() => discover({ modules: { app: definition("app"), x: { configUrl: "" } } }), /must declare a non-empty configUrl/);
});

test("root dependency references accept params and merge them last", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { precedence: "root", modules: { app: definition("app"), x: { configUrl: xUrl, params: { mode: "compact" } } } };
    const configs = { [xUrl]: { precedence: "dependency", modules: { x: definition("x", { params: { retained: true, mode: "normal" } }) } } };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.equal(effective.precedence, "root");
    assert.deepEqual(effective.modules.x.params, { retained: true, mode: "compact" });
});

test("module route targets use the owning module asset namespace while route keys remain unchanged", async () => {
    const root = {
        modules: {
            app: definition("app", {
                routes: {
                    "/something": "/pages/index.js",
                    "/repository/{repositoryId}/projects/{projectId}/items": "/pages/items.js"
                },
                controller: "/js/module.js"
            })
        }
    };

    const graph = await discover(root);
    const module = graph.rootNode.config.modules.app;

    assert.deepEqual(plain(module.routes), {
        "/something": "/_assets/app/1.0.0.dev/pages/index.js",
        "/repository/{repositoryId}/projects/{projectId}/items": "/_assets/app/1.0.0.dev/pages/items.js"
    });
    assert.equal(module.controller, "/_assets/app/1.0.0.dev/js/module.js");
});

test("module route targets are normalized independently for dependency modules", async () => {
    const dependencyUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(dependencyUrl) } };
    const configs = {
        [dependencyUrl]: {
            modules: {
                x: definition("x", {
                    routes: {
                        "/something": "/pages/index.js",
                        "/repository/{repositoryId}": "./pages/repository.js"
                    }
                })
            }
        }
    };

    const graph = await discover(root, configs);
    const module = graph.nodesById.get("x").config.modules.x;

    assert.deepEqual(plain(module.routes), {
        "/something": "/_assets/x/1.0.0.dev/pages/index.js",
        "/repository/{repositoryId}": "/_assets/x/1.0.0.dev/pages/repository.js"
    });
});

test("modules without routes remain without a routes property", async () => {
    const graph = await discover({ modules: { app: definition("app") } });

    assert.equal(Object.hasOwn(graph.rootNode.config.modules.app, "routes"), false);
});

test("root references reject assetsUrl but accept arbitrary composition properties", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: { configUrl: xUrl, version: "override" } } };
    const graph = await discover(root, { [xUrl]: { modules: { x: definition("x", { version: "dependency" }) } } });
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.equal(effective.modules.x.version, "override");
    await assert.rejects(() => discover({ modules: { app: definition("app"), x: { configUrl: xUrl, assetsUrl: "url:./assets" } } }), /cannot override assetsUrl/);
});

test("sibling contributions to a shared dependency follow declaration order", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = {
        modules: {
            app: definition("app"),
            a: reference(aUrl),
            b: reference(bUrl)
        }
    };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: { configUrl: xUrl, params: { mode: "a" } } } },
        [bUrl]: { modules: { b: definition("b"), x: { configUrl: xUrl, params: { mode: "b" } } } },
        [xUrl]: { modules: { x: definition("x", { params: { mode: "definition" } }) } }
    };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.deepEqual(orderOf(graph), ["x", "a", "b", "app"]);
    assert.equal(effective.modules.x.params.mode, "b");
});

test("root contributions to a shared dependency have final precedence", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = {
        modules: {
            app: definition("app"),
            a: reference(aUrl),
            x: { configUrl: xUrl, params: { mode: "root" } }
        }
    };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: { configUrl: xUrl, params: { mode: "child" } } } },
        [xUrl]: { modules: { x: definition("x", { params: { mode: "definition" } }) } }
    };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.deepEqual(orderOf(graph), ["x", "a", "app"]);
    assert.equal(effective.modules.x.params.mode, "root");
});

test("diamond dependency order is deterministic and follows sibling declaration order", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { trace: ["root"], modules: { app: definition("app"), b: reference(bUrl), a: reference(aUrl) } };
    const configs = {
        [aUrl]: { trace: ["a"], modules: { a: definition("a"), x: reference(xUrl) } },
        [bUrl]: { trace: ["b"], modules: { b: definition("b"), x: reference(xUrl) } },
        [xUrl]: { trace: ["x"], modules: { x: definition("x") } }
    };
    const graph = await discover(root, configs);
    const effective = plain(api.mergeConfigs(Array.from(graph.mergeOrder, node => node.config)));

    assert.deepEqual(orderOf(graph), ["x", "b", "a", "app"]);
    assert.deepEqual(effective.trace, ["x", "b", "a", "root"]);
});

test("dependency cycles are rejected with the cycle path", async () => {
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const root = { modules: { a: definition("a"), b: reference(bUrl) } };
    const configs = { [bUrl]: { modules: { b: definition("b"), a: reference(rootUrl) } } };

    await assert.rejects(() => discover(root, configs), /Module dependency cycle detected: 'a' -> 'b' -> 'a'/);
});
