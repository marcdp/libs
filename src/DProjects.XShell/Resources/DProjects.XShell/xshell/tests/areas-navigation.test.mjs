import assert from "node:assert/strict";
import test from "node:test";

import Areas from "../areas.js";
import Navigation from "../navigation.js";

test("documentation inventory menus keep Markdown page and index destinations", () => {
    const areas = new Areas({ config: { xshell: { areas: { definitions: {}, global: [] } } }, bus: { addEventListener() {} } });
    const root = "/_assets/xshell-docs/pages";
    const files = ["index.md", "10-architecture/index.md", "10-architecture/100-services.md"].map(path => ({ path: `${root}/${path}` }));
    const menu = areas._createMenuFromModuleFiles(files, root, "Docs", [".js", ".html", ".md"], true);
    assert.equal(menu[0].href, `${root}/index.md`);
    assert.equal(menu[0].children[0].href, `${root}/10-architecture/index.md`);
    assert.equal(menu[0].children[0].children[0].href, `${root}/10-architecture/100-services.md`);
});

test("Areas identifies modules under a normalized multi-segment assetsBase", () => {
    const config = { xshell: { assetsBase: "/virtual/assets", areas: { definitions: {}, global: [] } } };
    const areas = new Areas({ config, bus: { addEventListener() {} } });
    assert.equal(areas.getModuleId("/demo/virtual/assets/x/pages/index.js"), "x");
    assert.equal(areas.getModuleId("/demo/_assets/x/pages/index.js"), null);
});

function createAreas() {
    const bus = {
        addEventListener() {},
        emit() {}
    };
    const config = {
        xshell: {
            assetsBase: "/_assets",
            areas: {
                default: "sales",
                global: [],
                definitions: {
                    sales: { prefix: "/sales", modules: ["customer"] },
                    admin: { prefix: "/admin", modules: ["customer"] },
                    main: { prefix: "/main", modules: ["shell"] },
                    demo: { prefix: "/demo", modules: ["x-demo"] }
                }
            }
        }
    };
    const module = {
        id: "customer",
        routes: {
            "/customer/{customerId}": "/_assets/customer/pages/detail.js",
            "/customer-route/{customerId}": "/_assets/customer/pages/route-detail.js"
        },
        config: {
            menus: {
                navigation: [{
                    label: "Customers",
                    children: [{ label: "Detail", path: "/detail", href: "/_assets/customer/pages/detail.js" }]
                }],
                tools: [{ label: "Tool detail", path: "/tool-detail", href: "/_assets/customer/pages/tool-detail.js" }],
                canonical: [{ label: "Canonical only", href: "/_assets/customer/pages/canonical.js" }]
            }
        }
    };
    const demoModule = {
        id: "x-demo",
        routes: {
            "/something": "/_assets/x-demo/pages/about.js",
            "/repository/{repositoryId}": "/_assets/x-demo/pages/repository.js?mode=list",
            "/repository/{repositoryId}/projects/{projectId}/items": "/_assets/x-demo/pages/items.js",
            "/repository/{repositoryId}/items": "/_assets/x-demo/pages/items.js",
            "/navigation": "/_assets/x-demo/pages/route-navigation.js",
            "/navigation-route": "/_assets/x-demo/pages/03-navigation/index.js"
        },
        config: { menus: { navigation: "demo-runtime-menu-source" } }
    };
    const shellModule = { id: "shell", config: { menus: {} } };
    const areas = new Areas({ config, bus });
    areas.registerSource("demo-runtime-menu-source", {
        resolve() {
            return [{
                label: "Demo",
                path: "/",
                href: "/_assets/x-demo/pages/index.js",
                children: [{ label: "Navigation", path: "/navigation", href: "/_assets/x-demo/pages/03-navigation/index.js" }]
            }];
        }
    });
    const modules = new Map([[module.id, module], [demoModule.id, demoModule], [shellModule.id, shellModule]]);
    areas.init({ modules: { getModuleById(id) { return modules.get(id) || null; } } });
    return areas;
}

function createNavigation({ areas = createAreas(), mode = "path", basePath = "https://example.test/" } = {}) {
    return new Navigation({
        areas,
        bus: { emit() {} },
        config: {
            app: { basePath },
            xshell: { navigation: { mode, hashPrefix: "#!" } }
        },
        container: { querySelectorAll() { return []; } }
    });
}

function createSharedRouteContext({ definitions, defaultArea, mode = "path", basePath = "https://example.test/" }) {
    const config = {
        xshell: {
            assetsBase: "/_assets",
            areas: { default: defaultArea, global: [], definitions }
        }
    };
    const module = {
        id: "shared",
        routes: { "/repository/{repositoryId}/items": "/_assets/shared/pages/items.js" },
        config: { menus: {} }
    };
    const areas = new Areas({ config, bus: { addEventListener() {}, emit() {} } });
    areas.init({ modules: { getModuleById(id) { return id === module.id ? module : null; } } });
    return { areas, navigation: createNavigation({ areas, mode, basePath }), module };
}

function createNoRouteContext() {
    const config = {
        xshell: {
            assetsBase: "/_assets",
            areas: {
                default: "empty",
                global: [],
                definitions: { empty: { prefix: "/empty", modules: ["empty"] } }
            }
        }
    };
    const module = { id: "empty", config: { menus: {} } };
    const areas = new Areas({ config, bus: { addEventListener() {}, emit() {} } });
    areas.init({ modules: { getModuleById(id) { return id === module.id ? module : null; } } });
    return { areas, navigation: createNavigation({ areas }) };
}

test("Areas.resolveHref searches recursively across selected Area menus and ignores query and fragment", () => {
    const areas = createAreas();

    assert.equal(areas.resolveHref("/sales/_assets/customer/pages/detail.js?customer=42#summary", "sales")?.path, "/sales/detail");
    assert.equal(areas.resolveHref("/sales/_assets/customer/pages/tool-detail.js#actions", "sales")?.path, "/sales/tool-detail");
    assert.equal(areas.resolveHref("/sales/_assets/customer/pages/missing.js", "sales"), null);
});

test("Areas compose route descriptors in Area module and declaration order", () => {
    const areas = createRouteAreas();

    assert.deepEqual(areas.getArea("combined").routes, [
        { path: "/items", href: "/_assets/first/pages/items.js", module: "first" },
        { path: "/repository/{repositoryId}", href: "/_assets/first/pages/repository.js", module: "first" },
        { path: "/items", href: "/_assets/second/pages/items.js", module: "second" },
        { path: "/settings", href: "/_assets/second/pages/settings.js", module: "second" }
    ]);
});

test("Areas preserve route ownership and participation across multiple Areas", () => {
    const areas = createRouteAreas();

    assert.deepEqual(areas.getArea("first-only").routes, [
        { path: "/items", href: "/_assets/first/pages/items.js", module: "first" },
        { path: "/repository/{repositoryId}", href: "/_assets/first/pages/repository.js", module: "first" }
    ]);
    assert.deepEqual(areas.getArea("outsider-only").routes, [
        { path: "/outsider", href: "/_assets/outsider/pages/index.js", module: "outsider" }
    ]);
    assert.equal(areas.getArea("combined").routes.some(route => route.module === "outsider"), false);
});

test("Area routes are immutable and do not apply the Area prefix", () => {
    const areas = createRouteAreas();
    const area = areas.getArea("combined");

    assert.equal(Object.isFrozen(area.routes), true);
    assert.equal(Object.isFrozen(area.routes[0]), true);
    assert.equal(area.routes[0].path, "/items");
    assert.equal(area.routes[0].href, "/_assets/first/pages/items.js");
});

test("Navigation forward ambiguity follows composed Area module and route declaration order", () => {
    const areas = createRouteAreas();
    const navigation = createNavigation({ areas });

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/items")),
        "/demo/_assets/first/pages/items.js"
    );
});

test("Areas.resolveHref keeps ambiguous canonical Pages scoped to the selected Area", () => {
    const areas = createAreas();

    assert.equal(areas.resolveHref("/sales/_assets/customer/pages/detail.js", "sales")?.path, "/sales/detail");
    assert.equal(areas.resolveHref("/admin/_assets/customer/pages/detail.js", "admin")?.path, "/admin/detail");
    assert.equal(areas.resolveHref("/admin/_assets/customer/pages/detail.js", "sales"), null);
});

test("Areas.resolveHref discovers named dynamic menus through module-associated Areas", () => {
    const areas = createAreas();

    assert.equal(areas.resolveHref("/_assets/x-demo/pages/03-navigation/index.js")?.path, "/demo/navigation");
    assert.equal(areas.resolveHref("/_assets/x-demo/pages/03-navigation/index.js", "main"), null);
    assert.equal(areas.resolveHref("/_assets/x-demo/pages/index.js")?.path, "/demo/");
});

test("Areas.resolvePath retains its existing global exact-match behavior", () => {
    const areas = createAreas();

    assert.equal(areas.resolvePath("/sales/detail")?.href, "/sales/_assets/customer/pages/detail.js");
    assert.equal(areas.resolvePath("/sales/detail?customer=42"), null);
});

test("Navigation exposes a friendly path while preserving canonical lookup suffixes", () => {
    const navigation = createNavigation();
    const page = { src: "/sales/_assets/customer/pages/origin.js" };

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/detail.js?customer=42#summary", page }),
        "/sales/detail?customer=42#summary"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/detail.js#summary", params: { customer: 42 }, page }),
        "/sales/detail?customer=42#summary"
    );
});

function createRouteAreas() {
    const bus = {
        addEventListener() {},
        emit() {}
    };
    const config = {
        xshell: {
            assetsBase: "/_assets",
            areas: {
                default: "combined",
                global: [],
                definitions: {
                    combined: { prefix: "/demo", modules: ["first", "second"] },
                    "first-only": { prefix: "/first", modules: ["first"] },
                    "outsider-only": { prefix: "/outsider", modules: ["outsider"] }
                }
            }
        }
    };
    const modules = new Map([
        ["first", { id: "first", routes: {
            "/items": "/_assets/first/pages/items.js",
            "/repository/{repositoryId}": "/_assets/first/pages/repository.js"
        }, config: { menus: {} } }],
        ["second", { id: "second", routes: {
            "/items": "/_assets/second/pages/items.js",
            "/settings": "/_assets/second/pages/settings.js"
        }, config: { menus: {} } }],
        ["outsider", { id: "outsider", routes: { "/outsider": "/_assets/outsider/pages/index.js" }, config: { menus: {} } }]
    ]);
    const areas = new Areas({ config, bus });
    areas.init({ modules: { getModuleById(id) { return modules.get(id) || null; } } });
    return areas;
}

test("Navigation falls back to Area-aware canonical hrefs and preserves items without paths", () => {
    const navigation = createNavigation();
    const page = { src: "/sales/_assets/customer/pages/origin.js" };

    assert.equal(navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/missing.js", page }), "/sales/_assets/customer/pages/missing.js");
    assert.equal(navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/canonical.js", page }), "/sales/_assets/customer/pages/canonical.js");
});

test("Navigation uses the originating Page Area instead of the current Area", () => {
    const navigation = createNavigation();
    const page = { src: "/admin/_assets/customer/pages/origin.js" };

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/detail.js", page }),
        "/admin/detail"
    );
    assert.equal(
        navigation._resolveCanonicalHref("/_assets/customer/pages/detail.js?customer=42#summary", page),
        "/admin/_assets/customer/pages/detail.js?customer=42#summary"
    );
    assert.equal(navigation.buildUrlAbsolute({ href: "/sales/_assets/customer/pages/detail.js", page }), "/sales/detail");
});

test("Navigation discovers a target module's Area when the originating Page belongs to another Area", () => {
    const navigation = createNavigation();
    const page = { src: "/main/_assets/shell/pages/origin.js" };

    assert.equal(navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/03-navigation/index.js", page }), "/demo/navigation");
    assert.equal(
        navigation._resolveCanonicalHref("/_assets/x-demo/pages/03-navigation/index.js", page),
        "/demo/_assets/x-demo/pages/03-navigation/index.js"
    );
});

test("Navigation formats friendly URLs through existing path and hash modes with AppBasePath", () => {
    const page = { src: "/sales/_assets/customer/pages/origin.js" };
    const pathNavigation = createNavigation({ mode: "path", basePath: "https://example.test/app/" });
    const hashNavigation = createNavigation({ mode: "hash", basePath: "https://example.test/app/" });

    assert.equal(pathNavigation.buildUrlAbsolute({ href: "/_assets/customer/pages/detail.js", page }), "/app/sales/detail");
    assert.equal(hashNavigation.buildUrlAbsolute({ href: "/_assets/customer/pages/detail.js", page }), "/app/#!/sales/detail");
});

test("Navigation still resolves incoming friendly paths to canonical Page hrefs", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/sales/detail?customer=42#summary")),
        "/sales/_assets/customer/pages/detail.js?customer=42#summary"
    );
});

test("Navigation resolves Area routes to canonical Page hrefs with path parameters", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/12/projects/7/items")),
        "/demo/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7"
    );
});

test("Navigation preserves target query, incoming query, and fragment with route parameters authoritative", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/12?repositoryId=999&sort=name&page=3#details")),
        "/demo/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12&sort=name&page=3#details"
    );
});

test("Navigation safely serializes decoded route parameter values", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/hello%20world")),
        "/demo/_assets/x-demo/pages/repository.js?mode=list&repositoryId=hello%20world"
    );
    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/abc%2Fdef")),
        "/demo/_assets/x-demo/pages/repository.js?mode=list&repositoryId=abc%2Fdef"
    );
});

test("Navigation matches only the resolved Area routes", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/sales/customer/12")),
        "/sales/_assets/customer/pages/detail.js?customerId=12"
    );
    assert.equal(navigation._buildUrlFinal(navigation.parseUrl("/demo/customer/12")), "/demo/customer/12");
});

test("Navigation gives exact menu paths precedence over matching routes", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/navigation")),
        "/demo/_assets/x-demo/pages/03-navigation/index.js"
    );
});

test("Navigation route resolution preserves direct canonical and unmatched fallback behavior", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/demo/_assets/x-demo/pages/items.js?repositoryId=12")),
        "/demo/_assets/x-demo/pages/items.js?repositoryId=12"
    );
    assert.equal(navigation._buildUrlFinal(navigation.parseUrl("/demo/not-a-route")), "/demo/not-a-route");
});

test("Navigation preserves fallback and canonical access when a module and Area have no routes", () => {
    const { areas, navigation } = createNoRouteContext();

    assert.deepEqual(areas.getArea("empty").routes, []);
    assert.doesNotThrow(() => navigation._buildUrlFinal(navigation.parseUrl("/empty/not-a-route")));
    assert.equal(navigation._buildUrlFinal(navigation.parseUrl("/empty/not-a-route")), "/empty/not-a-route");
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/empty/pages/index.js?tab=info#details" }),
        "/empty/_assets/empty/pages/index.js?tab=info#details"
    );
    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/empty/_assets/empty/pages/index.js?tab=info#details")),
        "/empty/_assets/empty/pages/index.js?tab=info#details"
    );
});

test("Navigation keeps malformed encoded route parameters distinct from normal no-match fallback", () => {
    const navigation = createNavigation();

    assert.throws(
        () => navigation._buildUrlFinal(navigation.parseUrl("/demo/repository/%E0%A4%A")),
        /Invalid encoded route parameter/
    );
    assert.doesNotThrow(() => navigation._buildUrlFinal(navigation.parseUrl("/demo/not-a-route")));
});

test("Navigation route resolution is mode-independent and keeps browser-facing route URLs", () => {
    const pathNavigation = createNavigation({ mode: "path", basePath: "https://example.test/app/" });
    const hashNavigation = createNavigation({ mode: "hash", basePath: "https://example.test/app/" });
    const href = "/demo/repository/12";

    assert.equal(pathNavigation.buildUrlAbsolute({ href }), "/app/demo/repository/12");
    assert.equal(hashNavigation.buildUrlAbsolute({ href }), "/app/#!/demo/repository/12");
    assert.equal(
        pathNavigation._buildUrlFinal(pathNavigation.parseUrl(href)),
        "/demo/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12"
    );
    assert.equal(
        hashNavigation._buildUrlFinal(hashNavigation.parseUrl(href)),
        "/demo/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12"
    );
});

test("Navigation reverse maps literal and parameterized canonical Page hrefs", () => {
    const navigation = createNavigation();

    assert.equal(navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/about.js" }), "/demo/something");
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12" }),
        "/demo/repository/12"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7" }),
        "/demo/repository/12/projects/7/items"
    );
});

test("Navigation reverse routing consumes route and intrinsic target params while preserving unused query and fragments", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({
            href: "/_assets/x-demo/pages/items.js#summary",
            params: { repositoryId: "12", projectId: "7", sort: "name", page: 3 }
        }),
        "/demo/repository/12/projects/7/items?sort=name&page=3#summary"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12&sort=name#details" }),
        "/demo/repository/12?sort=name#details"
    );
});

test("Navigation reverse routing encodes placeholder values as individual path segments", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/repository.js", params: { mode: "list", repositoryId: "hello world" } }),
        "/demo/repository/hello%20world"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/repository.js", params: { mode: "list", repositoryId: "abc/def" } }),
        "/demo/repository/abc%2Fdef"
    );
});

test("Navigation reverse routing skips candidates with missing parameters and continues in Area order", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/items.js?repositoryId=12" }),
        "/demo/repository/12/items"
    );
    const fallback = navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/items.js?projectId=7" });
    assert.equal(fallback, "/sales/_assets/x-demo/pages/items.js?projectId=7");
    assert.equal(fallback.includes("undefined"), false);
});

test("Navigation reverse routing uses the first applicable route for duplicate targets", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7" }),
        "/demo/repository/12/projects/7/items"
    );
});

test("Navigation reverse ambiguity uses Area order without ranking placeholder counts", () => {
    const navigation = createNavigation();
    const area = {
        prefix: "/demo",
        routes: [
            { path: "/repository/{repositoryId}/items", href: "/_assets/x-demo/pages/items.js", module: "x-demo" },
            { path: "/repository/{repositoryId}/projects/{projectId}/items", href: "/_assets/x-demo/pages/items.js", module: "x-demo" }
        ]
    };

    assert.deepEqual(
        navigation._resolvePublicRouteTarget(
            "/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7",
            {},
            area
        ),
        { href: "/demo/repository/12/items", params: { projectId: "7" } }
    );
});

test("Navigation reverse ambiguity skips an inapplicable first route and uses the next candidate", () => {
    const navigation = createNavigation();
    const area = {
        prefix: "/demo",
        routes: [
            { path: "/repository/{repositoryId}/items", href: "/_assets/x-demo/pages/items.js", module: "x-demo" },
            { path: "/projects/{projectId}/items", href: "/_assets/x-demo/pages/items.js", module: "x-demo" }
        ]
    };

    assert.deepEqual(
        navigation._resolvePublicRouteTarget("/_assets/x-demo/pages/items.js?projectId=7", {}, area),
        { href: "/demo/projects/7/items", params: {} }
    );
});

test("Navigation treats missing, null, undefined, and empty reverse parameters as inapplicable", () => {
    const navigation = createNavigation();
    const area = {
        prefix: "/demo",
        routes: [{ path: "/repository/{repositoryId}/items", href: "/_assets/x-demo/pages/items.js", module: "x-demo" }]
    };

    for (const params of [{}, { repositoryId: null }, { repositoryId: undefined }, { repositoryId: "" }]) {
        assert.doesNotThrow(() => navigation._resolvePublicRouteTarget("/_assets/x-demo/pages/items.js", params, area));
        assert.equal(navigation._resolvePublicRouteTarget("/_assets/x-demo/pages/items.js", params, area), null);
    }
});

test("Navigation reverse routing keeps exact menu aliases ahead of routes", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/03-navigation/index.js" }),
        "/demo/navigation"
    );
});

test("Navigation reverse routing honors originating Area and target-module Area discovery", () => {
    const navigation = createNavigation();
    const page = { src: "/admin/_assets/customer/pages/origin.js" };

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/customer/pages/route-detail.js?customerId=12", page }),
        "/admin/customer-route/12"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/about.js" }),
        "/demo/something"
    );
});

test("Navigation reverse routing applies an existing Area prefix exactly once", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/demo/_assets/x-demo/pages/about.js" }),
        "/demo/something"
    );
});

test("Navigation keeps Area routes relative while applying a non-root Area prefix exactly once in both directions", () => {
    const navigation = createNavigation();
    const publicHref = "/demo/repository/12/projects/7/items?sort=name#details";
    const canonicalHref = "/demo/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#details";

    assert.equal(navigation._buildUrlFinal(navigation.parseUrl(publicHref)), canonicalHref);
    assert.equal(navigation.buildUrlAbsolute({ href: canonicalHref }), publicHref);
    assert.equal(navigation.buildUrlAbsolute({ href: canonicalHref }).includes("/demo/demo/"), false);
    assert.equal(navigation._areas.getArea("demo").routes[2].path, "/repository/{repositoryId}/projects/{projectId}/items");
});

test("Navigation supports forward and reverse routes in an empty-prefix root Area", () => {
    const { areas, navigation, module } = createSharedRouteContext({
        defaultArea: "root",
        definitions: { root: { prefix: "", modules: ["shared"] } }
    });
    const publicHref = "/repository/12/items?sort=name#details";
    const canonicalHref = "/_assets/shared/pages/items.js?repositoryId=12&sort=name#details";

    assert.equal(navigation._buildUrlFinal(navigation.parseUrl(publicHref)), canonicalHref);
    assert.equal(navigation.buildUrlAbsolute({ href: canonicalHref }), publicHref);
    assert.equal(navigation.buildUrlAbsolute({ href: canonicalHref }).startsWith("//"), false);
    assert.deepEqual(module.routes, { "/repository/{repositoryId}/items": "/_assets/shared/pages/items.js" });
    assert.equal(areas.getArea("root").routes[0].path, "/repository/{repositoryId}/items");
});

test("Navigation preserves explicit and originating Area context when one module participates in multiple Areas", () => {
    const { navigation } = createSharedRouteContext({
        defaultArea: "sales",
        definitions: {
            sales: { prefix: "/sales", modules: ["shared"] },
            admin: { prefix: "/admin", modules: ["shared"] }
        }
    });
    const adminPage = { src: "/admin/_assets/shared/pages/origin.js" };

    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl("/admin/repository/12/items")),
        "/admin/_assets/shared/pages/items.js?repositoryId=12"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/admin/_assets/shared/pages/items.js?repositoryId=12" }),
        "/admin/repository/12/items"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/shared/pages/items.js?repositoryId=12", page: adminPage }),
        "/admin/repository/12/items"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/shared/pages/items.js?repositoryId=12" }),
        "/sales/repository/12/items"
    );
    assert.equal(
        navigation.buildUrlAbsolute({ href: "/admin/_assets/shared/pages/items.js" }),
        "/admin/_assets/shared/pages/items.js"
    );
});

test("Navigation wraps root and prefixed route URLs through path and hash AppBasePath modes", () => {
    const definitions = { demo: { prefix: "/demo", modules: ["shared"] } };
    const path = createSharedRouteContext({ definitions, defaultArea: "demo", mode: "path", basePath: "https://example.test/app/" }).navigation;
    const hash = createSharedRouteContext({ definitions, defaultArea: "demo", mode: "hash", basePath: "https://example.test/app/" }).navigation;
    const canonicalHref = "/_assets/shared/pages/items.js?repositoryId=12&sort=name#details";

    assert.equal(path.buildUrlAbsolute({ href: canonicalHref }), "/app/demo/repository/12/items?sort=name#details");
    assert.equal(hash.buildUrlAbsolute({ href: canonicalHref }), "/app/#!/demo/repository/12/items?sort=name#details");
});

test("Navigation reverse routing requires intrinsic target query parameters", () => {
    const navigation = createNavigation();

    assert.equal(
        navigation.buildUrlAbsolute({ href: "/_assets/x-demo/pages/repository.js?mode=grid&repositoryId=12" }),
        "/sales/_assets/x-demo/pages/repository.js?mode=grid&repositoryId=12"
    );
});

test("Navigation reverse routes consistently in path and hash modes with AppBasePath", () => {
    const pathNavigation = createNavigation({ mode: "path", basePath: "https://example.test/app/" });
    const hashNavigation = createNavigation({ mode: "hash", basePath: "https://example.test/app/" });
    const href = "/_assets/x-demo/pages/repository.js?mode=list&repositoryId=12";

    assert.equal(pathNavigation.buildUrlAbsolute({ href }), "/app/demo/repository/12");
    assert.equal(hashNavigation.buildUrlAbsolute({ href }), "/app/#!/demo/repository/12");
});

test("Navigation round-trips canonical route identity with unused query and fragment", () => {
    const navigation = createNavigation();
    const canonical = "/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#summary";
    const publicHref = navigation.buildUrlAbsolute({ href: canonical });

    assert.equal(publicHref, "/demo/repository/12/projects/7/items?sort=name#summary");
    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl(publicHref)),
        "/demo/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#summary"
    );
});

test("Navigation round-trips encoded route segments without changing segment boundaries", () => {
    const navigation = createNavigation();
    const canonical = "/_assets/x-demo/pages/items.js?repositoryId=hello%20world&projectId=abc%2Fdef&sort=name%20asc#details";
    const publicHref = navigation.buildUrlAbsolute({ href: canonical });

    assert.equal(publicHref, "/demo/repository/hello%20world/projects/abc%2Fdef/items?sort=name%20asc#details");
    assert.equal(
        navigation._buildUrlFinal(navigation.parseUrl(publicHref)),
        "/demo/_assets/x-demo/pages/items.js?repositoryId=hello%20world&projectId=abc%2Fdef&sort=name%20asc#details"
    );
});

test("Navigation round-trips routes through an empty-prefix Area", () => {
    const { navigation } = createSharedRouteContext({
        defaultArea: "root",
        definitions: { root: { prefix: "", modules: ["shared"] } }
    });
    const canonical = "/_assets/shared/pages/items.js?repositoryId=12&sort=name#details";
    const publicHref = navigation.buildUrlAbsolute({ href: canonical });

    assert.equal(publicHref, "/repository/12/items?sort=name#details");
    assert.equal(navigation._buildUrlFinal(navigation.parseUrl(publicHref)), canonical);
});

test("Navigation round-trips routes through path and hash modes with AppBasePath", () => {
    const canonical = "/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#summary";
    const expected = "/demo/_assets/x-demo/pages/items.js?repositoryId=12&projectId=7&sort=name#summary";
    const cases = [
        { navigation: createNavigation({ mode: "path", basePath: "https://example.test/app/" }), browserPrefix: "/app" },
        { navigation: createNavigation({ mode: "hash", basePath: "https://example.test/app/" }), browserPrefix: "/app/#!" }
    ];

    for (const { navigation, browserPrefix } of cases) {
        const publicHref = navigation.buildUrlAbsolute({ href: canonical });
        const applicationHref = publicHref.substring(browserPrefix.length);

        assert.equal(navigation._buildUrlFinal(navigation.parseUrl(applicationHref)), expected);
    }
});

test("Navigation leaves external URLs untouched", () => {
    const navigation = createNavigation();
    const page = { src: "/sales/_assets/customer/pages/origin.js" };

    for (const href of ["https://example.com/a?b=1#c", "http://example.com", "mailto:user@example.com", "tel:+123456789"]) {
        assert.equal(navigation.buildUrlAbsolute({ href, page }), href);
    }
});
