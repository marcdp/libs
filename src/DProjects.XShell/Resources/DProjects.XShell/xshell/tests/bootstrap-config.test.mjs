import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";

import Modules from "../modules.js";
import Resolver from "../resolver.js";

const rootUrl = "https://example.test/modules/app/module.jsonc";
const bootstrapPath = new URL("../bootstrap.js", import.meta.url);
const bootstrapSource = readFileSync(bootstrapPath, "utf8").replace(
    /\/\/ exec bootstrap\s*bootstrap\(\);\s*$/,
    `globalThis.__bootstrapTests = {
        getLocalModule: (config, configUrl) => getLocalModule(JSON.parse(JSON.stringify(config)), configUrl),
        discover: (rootConfig, rootConfigUrl, configs, calls) => discoverModuleConfigs(
            JSON.parse(JSON.stringify(rootConfig)),
            rootConfigUrl,
            async configUrl => {
                calls.push(configUrl);
                if (!Object.hasOwn(configs, configUrl)) throw new Error(\`Missing test config: \${configUrl}\`);
                return JSON.parse(JSON.stringify(configs[configUrl]));
            },
            "_assets"
        ),
        mergeConfigs,
        loadConfig,
        loadFilesIndexes,
        fillResolverRules,
        initializeXShell
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

test("generated Markdown Page rules preserve document URLs and cache classes by path", () => {
    const config = {
        app: { basePath: "/app" },
        modules: { "xshell-docs": { assetsPath: "/_assets/xshell-docs", files: [] }, sample: { assetsPath: "/_assets/sample", files: [] } },
        xshell: { resolver: {} }
    };
    api.fillResolverRules(config);
    const resolver = new Resolver({ config });
    for (const moduleId of ["xshell-docs", "sample"]) {
        const path = `/_assets/${moduleId}/pages/10-architecture/100-services.md`;
        const result = resolver.resolve(`page:${path}?mode=one#section`);
        assert.equal(result.url, "/app" + path);
        assert.equal(result.definition.loader, "page-md");
        assert.equal(result.definition.cache, true);
        assert.equal(result.definition.cacheMode, "path");
        assert.equal(result.definition.cacheMode, config.xshell.resolver.page[`/_assets/${moduleId}/{path}.js`].cacheMode);
    }
});

const definition = (value, extra = {}) => ({ label: value, value, ...extra });
const reference = configUrl => ({ configUrl });
const plain = value => JSON.parse(JSON.stringify(value));
async function discover(root, configs = {}, calls = []) {
    return api.discover(root, rootUrl, configs, calls);
}
function orderOf(graph) {
    return Array.from(graph.mergeOrder, node => node.id);
}

test("loadFilesIndexes concurrently loads module and XShell inventories through the virtual namespace", async () => {
    const xUrl = "https://example.test/app/_assets/x/module.files.json";
    const reportsUrl = "https://example.test/app/_assets/reports/module.files.json";
    const xshellUrl = "https://example.test/app/_assets/xshell/module.files.json";
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
        modules: { x: { assetsPath: "/_assets/x" }, reports: { assetsPath: "/_assets/reports" } },
        xshell: { assetsPrefix: "_assets", assetsPath: "/_assets/xshell" }
    };

    const loading = api.loadFilesIndexes(config);
    await Promise.resolve();

    assert.deepEqual([...pending.keys()], [xUrl, reportsUrl, xshellUrl]);
    pending.get(xUrl)(response([{ path: "/components/x-button.js", size: 1234, hash: "x-hash" }]));
    pending.get(reportsUrl)(response([{ path: "/pages/home.js", size: 25, hash: "reports-hash" }]));
    pending.get(xshellUrl)(response([{ path: "/xshell.js", size: 5678, hash: "xshell-hash" }]));
    await loading;

    assert.deepEqual(plain(config.modules.x.files), [{ path: "/_assets/x/components/x-button.js", size: 1234, hash: "x-hash" }]);
    assert.deepEqual(plain(config.modules.reports.files), [{ path: "/_assets/reports/pages/home.js", size: 25, hash: "reports-hash" }]);
    assert.deepEqual(plain(config.xshell.files), [{ path: "/_assets/xshell/xshell.js", size: 5678, hash: "xshell-hash" }]);
    for (const url of [xUrl, reportsUrl, xshellUrl]) fetchOverrides.delete(url);
});

test("loadFilesIndexes reports the inventory id, URL, and HTTP failure", async () => {
    const inventoryUrl = "https://example.test/app/_assets/x/module.files.json";
    const xshellInventoryUrl = "https://example.test/app/_assets/xshell/module.files.json";
    fetchOverrides.set(inventoryUrl, async () => ({ ok: false, status: 503, statusText: "Service Unavailable" }));
    fetchedResources.set(xshellInventoryUrl, []);
    const config = { modules: { x: { assetsPath: "/_assets/x" } }, xshell: { assetsPrefix: "_assets", assetsPath: "/_assets/xshell" } };

    await assert.rejects(
        () => api.loadFilesIndexes(config),
        error => error.message.includes("'x'") && error.message.includes(inventoryUrl) && error.message.includes("503 Service Unavailable")
    );
    fetchOverrides.delete(inventoryUrl);
    fetchedResources.delete(xshellInventoryUrl);
});

test("loadFilesIndexes fails clearly when the XShell inventory is missing", async () => {
    const inventoryUrl = "https://example.test/app/_assets/xshell/module.files.json";
    const config = { modules: {}, xshell: { assetsPrefix: "_assets", assetsPath: "/_assets/xshell" } };

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
            url: "/_assets/x/contracts/toast.json",
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
            resolver: { module: { xshell: { url: "/_assets/xshell/xshell.js" } } }
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

test("loadConfig keeps xshellConfig as the base and applies root configuration last", async () => {
    directFetchCalls.length = 0;
    const xshellUrl = "https://example.test/xshell/xshell.jsonc";
    fetchedResources.set(xshellUrl, {
        app: { source: "xshell" },
        modules: {},
        xshell: { assetsPrefix: "_assets", environment: "Production", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, {
        app: { source: "root" },
        modules: { app: definition("app") }
    });
    const config = plain(await api.loadConfig());

    assert.equal(config.app.source, "root");
    assert.equal(config.app.basePath, "https://example.test/app");
    assert.deepEqual(config.app.params, { mode: "host" });
    assert.deepEqual(config.modules.app.params, { mode: "host" });
    assert.equal(config.modules.app.assetsPath, "/_assets/app");
    assert.equal(config.xshell.configUrl, xshellUrl);
    assert.equal(config.xshell.assetsPath, "/_assets/xshell");
    assert.deepEqual(directFetchCalls, [xshellUrl, rootUrl]);
});

test("loadConfig derives every assetsPath from a custom assetsPrefix", async () => {
    const xshellUrl = "https://example.test/xshell/xshell.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    fetchedResources.set(xshellUrl, {
        app: {},
        modules: {},
        xshell: { assetsPrefix: "runtime", assetsPath: "/authored-xshell", environment: "Production", assetsUrl: "url:./", temp: { url: "url:./" }, resolver: {} }
    });
    fetchedResources.set(rootUrl, {
        modules: { app: definition("app", { assetsPath: "/authored-app" }), x: reference(xUrl) }
    });
    fetchedResources.set(xUrl, { modules: { x: definition("x", { assetsPath: "/authored-x" }) } });

    const config = plain(await api.loadConfig());

    assert.equal(config.modules.app.assetsPath, "/runtime/app");
    assert.equal(config.modules.x.assetsPath, "/runtime/x");
    assert.equal(config.xshell.assetsPath, "/runtime/xshell");
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
        config: { xshell: { assetsPrefix: "_assets" }, modules: effective.modules },
        loader: { load() { throw new Error("No resources should be loaded by this fixture."); } },
        resolver: {},
        document: { adoptedStyleSheets: [] },
        services: {}
    });
    await modules.init();
    assert.equal(modules.getModules().filter(module => module.id === "x").length, 1);
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
        "/something": "/_assets/app/pages/index.js",
        "/repository/{repositoryId}/projects/{projectId}/items": "/_assets/app/pages/items.js"
    });
    assert.equal(module.controller, "/_assets/app/js/module.js");
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
        "/something": "/_assets/x/pages/index.js",
        "/repository/{repositoryId}": "/_assets/x/pages/repository.js"
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
