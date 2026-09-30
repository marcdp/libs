import assert from "node:assert/strict";
import test from "node:test";

import Areas from "../areas.js";
import Navigation from "../navigation.js";

function createAreas() {
    const bus = {
        addEventListener() {},
        emit() {}
    };
    const config = {
        xshell: {
            areas: {
                default: "sales",
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
        config: { menus: { navigation: "x-demo-dynamic-navigation-menu-source" } }
    };
    const shellModule = { id: "shell", config: { menus: {} } };
    const areas = new Areas({ config, bus });
    areas.registerSource("x-demo-dynamic-navigation-menu-source", {
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
            assetsPrefix: "_assets",
            areas: {
                default: "combined",
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

test("Navigation leaves external URLs untouched", () => {
    const navigation = createNavigation();
    const page = { src: "/sales/_assets/customer/pages/origin.js" };

    for (const href of ["https://example.com/a?b=1#c", "http://example.com", "mailto:user@example.com", "tel:+123456789"]) {
        assert.equal(navigation.buildUrlAbsolute({ href, page }), href);
    }
});
