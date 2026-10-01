import assert from "node:assert/strict";
import test from "node:test";

import Navigation from "../navigation.js";

function createNavigation() {
    return new Navigation({
        areas: {},
        bus: {},
        config: {
            app: { basePath: "https://example.test/" },
            xshell: { navigation: { mode: "path", hashPrefix: "#!" } }
        },
        container: {}
    });
}

test("route compiler handles literal paths", () => {
    const route = createNavigation()._compileRoute("/something");

    assert.deepEqual(route.parameters, []);
    assert.equal(route.matcher.test("/something"), true);
    assert.equal(route.matcher.test("/other"), false);
});

test("route compiler preserves parameter declaration order", () => {
    const route = createNavigation()._compileRoute("/repository/{repositoryId}/projects/{projectId}/items");

    assert.deepEqual(route.parameters, ["repositoryId", "projectId"]);
    assert.equal(route.matcher.test("/repository/12/projects/7/items"), true);
    assert.equal(route.matcher.test("/repository/12/projects/7/items/extra"), false);
});

test("route compiler escapes literal regex characters and parameters match one segment", () => {
    const route = createNavigation()._compileRoute("/v1.0/items/{id}");

    assert.equal(route.matcher.test("/v1.0/items/a"), true);
    assert.equal(route.matcher.test("/v1x0/items/a"), false);
    assert.equal(route.matcher.test("/v1.0/items/a/b"), false);
});

test("route compiler rejects malformed and unsupported patterns", () => {
    const navigation = createNavigation();

    for (const path of [
        "/repository/{}",
        "/repository/{123id}",
        "/repository/{repository-id}",
        "/repository/{id}/thing-{other}",
        "/repository/{id",
        "/repository/{id}/items/{id}",
        "/repository/*",
        "/repository/{id}?value"
    ]) {
        assert.throws(() => navigation._compileRoute(path), /Invalid route path/);
    }
});

test("Area route matching returns the first matching route and extracted parameters", () => {
    const area = {
        routes: [
            { path: "/repository/{repositoryId}", href: "/_assets/repository/pages/index.js", module: "repository" },
            { path: "/repository/{repositoryId}/projects/{projectId}/items", href: "/_assets/repository/pages/items.js", module: "repository" }
        ]
    };
    const result = createNavigation()._matchAreaRoutes("/repository/12/projects/7/items", area);

    assert.equal(result.route, area.routes[1]);
    assert.deepEqual(result.params, { repositoryId: "12", projectId: "7" });
});

test("Area route matching preserves declaration order for duplicate matches", () => {
    const first = { path: "/items", href: "/_assets/first/pages/items.js", module: "first" };
    const second = { path: "/items", href: "/_assets/second/pages/items.js", module: "second" };

    const result = createNavigation()._matchAreaRoutes("/items", { routes: [first, second] });

    assert.equal(result.route, first);
});

test("Area route matching uses the first parameterized candidate without ranking", () => {
    const first = { path: "/items/{id}", href: "/_assets/first/pages/item.js", module: "first" };
    const second = { path: "/items/{name}", href: "/_assets/second/pages/item.js", module: "second" };

    const result = createNavigation()._matchAreaRoutes("/items/123", { routes: [first, second] });

    assert.equal(result.route, first);
    assert.deepEqual(result.params, { id: "123" });
});

test("Area route matching does not prefer literal routes over earlier parameterized routes", () => {
    const parameterized = { path: "/items/{id}", href: "/_assets/first/pages/item.js", module: "first" };
    const literal = { path: "/items/special", href: "/_assets/second/pages/special.js", module: "second" };
    const navigation = createNavigation();

    assert.equal(navigation._matchAreaRoutes("/items/special", { routes: [parameterized, literal] }).route, parameterized);
    assert.equal(navigation._matchAreaRoutes("/items/special", { routes: [literal, parameterized] }).route, literal);
});

test("Area route matching ignores query strings and hash fragments", () => {
    const route = { path: "/repository/{repositoryId}", href: "/_assets/repository/pages/index.js", module: "repository" };
    const result = createNavigation()._matchAreaRoutes("/repository/12?tab=history#section", { routes: [route] });

    assert.deepEqual(result.params, { repositoryId: "12" });
});

test("Area route matching decodes each captured segment without changing boundaries", () => {
    const route = { path: "/repository/{repositoryId}", href: "/_assets/repository/pages/index.js", module: "repository" };
    const result = createNavigation()._matchAreaRoutes("/repository/abc%2Fdef", { routes: [route] });

    assert.deepEqual(result.params, { repositoryId: "abc/def" });
});

test("Area route matching returns null when no route matches", () => {
    const result = createNavigation()._matchAreaRoutes("/missing", { routes: [{ path: "/items", href: "/items.js", module: "test" }] });

    assert.equal(result, null);
});

test("Area route matching reports malformed percent encoding clearly", () => {
    const route = { path: "/repository/{repositoryId}", href: "/items.js", module: "test" };

    assert.throws(
        () => createNavigation()._matchAreaRoutes("/repository/%E0%A4%A", { routes: [route] }),
        /Invalid encoded route parameter/
    );
});

test("Area route matching does not mutate route descriptors or resolve hrefs", () => {
    const route = Object.freeze({ path: "/items", href: "/_assets/test/pages/items.js", module: "test" });
    const result = createNavigation()._matchAreaRoutes("/items", { routes: [route] });

    assert.deepEqual(result.route, route);
    assert.equal(result.route.href, "/_assets/test/pages/items.js");
    assert.deepEqual(result.params, {});
});
