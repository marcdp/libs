import assert from "node:assert/strict";
import test from "node:test";

import Modules from "../modules.js";
import Services from "../services.js";
import Loader from "../loader.js";
import Resolver from "../resolver.js";

function freeze(value) {
    if (value && typeof value === "object" && !Object.isFrozen(value)) {
        for (const child of Object.values(value)) freeze(child);
        Object.freeze(value);
    }
    return value;
}

function moduleDefinition(id, overrides = {}) {
    return {
        label: `${id} module`,
        assetsPath: `/_assets/${id}`,
        files: [],
        ...overrides
    };
}

function createModules(config, { loader = {}, services = {} } = {}) {
    return new Modules({
        bus: {},
        config,
        loader,
        resolver: {},
        document: { adoptedStyleSheets: [] },
        services
    });
}

test("module without service requirements initializes and starts normally", async () => {
    let starts = 0;
    class ModuleController {

        start() {
            starts++;
        }
    }
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: {
            orders: moduleDefinition("orders", { files: [{ path: "/_assets/orders/module.js" }] })
        }
    });
    const modules = createModules(config, {
        loader: { async load() { return ModuleController; } },
        services: { resolve() { throw new Error("no service should be resolved"); } }
    });

    await modules.init();

    assert.equal(starts, 1);
});

test("module registry is a frozen metadata snapshot of the runtime modules", async () => {
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: { orders: moduleDefinition("orders") }
    });
    const modules = createModules(config);
    const before = modules.registry;

    await modules.init();

    const registry = modules.registry;
    assert.deepEqual(before, []);
    const runtimeModule = modules.getModuleById("orders");
    assert.equal(Object.isFrozen(registry), true);
    assert.notStrictEqual(registry[0], runtimeModule);
    assert.equal(registry[0].id, runtimeModule.id);
    assert.equal(registry[0].path, runtimeModule.path);
    assert.equal(Object.hasOwn(registry[0], "controller"), false);
    assert.equal(Object.hasOwn(registry[0], "config"), false);
    assert.equal(Object.isFrozen(registry[0]), true);
    assert.equal(Object.isFrozen(registry[0].files), true);
    assert.throws(() => registry.push({ id: "other" }), TypeError);
    assert.throws(() => registry.splice(0, 1), TypeError);
    assert.notStrictEqual(modules.registry, registry);
    assert.deepEqual(modules.registry, registry);
});

test("runtime module path uses its effective assetsPath", async () => {
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: { orders: moduleDefinition("orders", { assetsPath: "/runtime/orders" }) }
    });
    const modules = createModules(config);

    await modules.init();

    const module = modules.getModuleById("orders");
    assert.equal(module.path, module.config.assetsPath);
    assert.equal(module.path, "/runtime/orders");
    assert.equal(modules.resolveModuleId("/runtime/orders/pages/index.js"), "orders");
});

test("module requirements accept configured lazy services without constructing them", async () => {
    let constructions = 0;
    class IdentityService {

        constructor() {
            constructions++;
        }
    }
    const servicePath = "/_assets/orders/services/identity.js";
    const config = freeze({
        app: { basePath: "/app" },
        xshell: {
            assetsPrefix: "_assets",
            services: { identity: { contract: "identity", implementation: servicePath } }
        },
        modules: {
            orders: moduleDefinition("orders", { requires: ["identity"], files: [{ path: servicePath, size: 1 }] })
        }
    });
    const loader = { async load(resource) {
        assert.equal(resource, `module:${servicePath}`);
        return IdentityService;
    } };
    const services = new Services({
        config,
        loader,
        contracts: {
            getContractItemById() {
                return { id: "identity", contract: { label: "Identity", methods: {}, properties: {} } };
            }
        },
        areas: { getModuleId() { return "orders"; } }
    });
    await services.init();
    const modules = createModules(config, { loader, services });

    await modules.init();

    assert.equal(services.getServiceInfo("identity").state, "registered");
    assert.equal(constructions, 0);
});

test("missing module requirement fails before its controller is loaded or started", async () => {
    const loads = [];
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: {
            orders: moduleDefinition("orders", { requires: ["identity"], files: [{ path: "/_assets/orders/module.js" }] })
        }
    });
    const modules = createModules(config, {
        loader: { async load(resource) { loads.push(resource); } },
        services: { has() { return false; } }
    });

    await assert.rejects(() => modules.init(), /Module 'orders' requires unavailable service 'identity'\./);
    assert.deepEqual(loads, []);
});

test("runtime module exposes normalized routes from its effective configuration", async () => {
    const routes = {
        "/something": "/_assets/x-demo/pages/index.js",
        "/repository/{repositoryId}/projects/{projectId}/items": "/_assets/x-demo/pages/items.js"
    };
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: {
            "x-demo": moduleDefinition("x-demo", { routes })
        }
    });
    const modules = createModules(config);

    await modules.init();

    const module = modules.getModuleById("x-demo");
    assert.equal(module.routes, routes);
    assert.equal(module.routes["/something"], "/_assets/x-demo/pages/index.js");
    assert.equal(module.routes["/repository/{repositoryId}/projects/{projectId}/items"], "/_assets/x-demo/pages/items.js");
    assert.equal(Object.isFrozen(module.routes), true);
});

test("runtime modules expose independent route definitions", async () => {
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: {
            first: moduleDefinition("first", { routes: { "/first": "/_assets/first/pages/index.js" } }),
            second: moduleDefinition("second", { routes: { "/second": "/_assets/second/pages/index.js" } })
        }
    });
    const modules = createModules(config);

    await modules.init();

    assert.deepEqual(modules.getModuleById("first").routes, { "/first": "/_assets/first/pages/index.js" });
    assert.deepEqual(modules.getModuleById("second").routes, { "/second": "/_assets/second/pages/index.js" });
    assert.notEqual(modules.getModuleById("first").routes, modules.getModuleById("second").routes);
});

test("runtime module without routes exposes a frozen empty object", async () => {
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: { "x-demo": moduleDefinition("x-demo") }
    });
    const modules = createModules(config);

    await modules.init();

    const routes = modules.getModuleById("x-demo").routes;
    assert.deepEqual(routes, {});
    assert.equal(Object.isFrozen(routes), true);
});

test("module styles use the Loader once per inventoried stylesheet and are adopted after startup", async () => {
    const paths = ["/_assets/first/styles/index.css", "/_assets/second/styles/index.css"];
    const requests = [];
    const events = [];
    globalThis.window = { location: { origin: "https://example.test" }, customElements: { get() {} } };
    globalThis.CSSStyleSheet = class {
        async replace(css) { this.css = css; }
    };
    globalThis.fetch = async url => {
        requests.push(url);
        return { ok: true, async text() { return url.includes("/first/") ? "first{color:red}" : "second{color:blue}"; } };
    };
    const styleLoaderUrl = new URL("../loaders/style-css.js", import.meta.url).href;
    const config = freeze({
        app: { basePath: "" },
        xshell: {
            assetsPrefix: "_assets",
            navigation: { mode: "path", hashPrefix: "#!" },
            ui: { component: { lazy: null } },
            resolver: { style: Object.fromEntries(["first", "second"].map(id => [
                `/_assets/${id}/{path}.css`,
                { url: `/_assets/${id}/{path}.css`, loader: styleLoaderUrl, cache: true, moduleId: id, modulePath: `/_assets/${id}` }
            ])) }
        },
        modules: {
            first: moduleDefinition("first", { files: [{ path: paths[0] }, { path: paths[0] }] }),
            second: moduleDefinition("second", { files: [{ path: paths[1] }] }),
            missing: moduleDefinition("missing")
        }
    });
    const resolver = new Resolver({ config });
    const loader = new Loader({ bus: { emit(name, payload) { events.push({ name, payload }); } }, config, resolver });
    const document = { adoptedStyleSheets: [] };
    const modules = new Modules({ bus: {}, config, loader, resolver, document, services: {} });

    await modules.init();

    const first = modules.getModuleById("first").styles;
    const second = modules.getModuleById("second").styles;
    assert.equal(first.length, 1);
    assert.equal(second.length, 1);
    assert.deepEqual(modules.getModuleById("missing").styles, []);
    assert.notStrictEqual(first[0], second[0]);
    assert.equal(first[0].css, "first{color:red}");
    assert.equal(second[0].css, "second{color:blue}");
    assert.deepEqual(document.adoptedStyleSheets, [first[0], second[0]]);
    assert.deepEqual(requests, paths.map(path => `https://example.test${path}`));
    assert.deepEqual(loader.registry.map(item => [item.resource, item.moduleId, item.status]), [
        [`style:${paths[0]}`, "first", "loaded"],
        [`style:${paths[1]}`, "second", "loaded"]
    ]);
    assert.equal(events.filter(event => event.name === "xshell:loader:resource:loaded").length, 2);
    assert.strictEqual(await loader.load(`style:${paths[0]}`), first[0]);
    assert.deepEqual(requests, paths.map(path => `https://example.test${path}`));
});
