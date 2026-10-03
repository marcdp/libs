import assert from "node:assert/strict";
import test from "node:test";

import Modules from "../modules.js";
import Services from "../services.js";

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

    assert.equal(services.getServiceItemById("identity").state, "registered");
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
