import assert from "node:assert/strict";
import test from "node:test";

import Modules from "../modules.js";

function freeze(value) {
    if (value && typeof value === "object" && !Object.isFrozen(value)) {
        for (const child of Object.values(value)) freeze(child);
        Object.freeze(value);
    }
    return value;
}

function createModules(config) {
    return new Modules({
        bus: {},
        config,
        loader: {},
        resolver: {},
        document: { adoptedStyleSheets: [] },
        services: {}
    });
}

test("runtime module exposes normalized routes from its effective configuration", async () => {
    const routes = {
        "/something": "/_assets/x-demo/pages/index.js",
        "/repository/{repositoryId}/projects/{projectId}/items": "/_assets/x-demo/pages/items.js"
    };
    const config = freeze({
        xshell: { assetsPrefix: "_assets" },
        modules: {
            "x-demo": { label: "Demo", routes }
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
            first: { routes: { "/first": "/_assets/first/pages/index.js" } },
            second: { routes: { "/second": "/_assets/second/pages/index.js" } }
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
        modules: { "x-demo": { label: "Demo" } }
    });
    const modules = createModules(config);

    await modules.init();

    const routes = modules.getModuleById("x-demo").routes;
    assert.deepEqual(routes, {});
    assert.equal(Object.isFrozen(routes), true);
});
