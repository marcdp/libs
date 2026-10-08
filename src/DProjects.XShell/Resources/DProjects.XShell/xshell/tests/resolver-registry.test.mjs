import assert from "node:assert/strict";
import test from "node:test";

import Resolver from "../resolver.js";
import Navigation from "../navigation.js";
import Areas from "../areas.js";

test("resolver registry snapshots expose rule metadata without mutable definitions", () => {
    const resolver = new Resolver({ config: { app: { basePath: "/app", baseUrl: "https://example.test/app/" }, xshell: { resolver: {} } } });
    const before = resolver.registry;
    assert.deepEqual(before, []);

    resolver.addDefinition("icon:{name}", { src: "/icons/{name}.svg", loader: "icon-svg", cache: true });
    const after = resolver.registry;
    assert.equal(Object.isFrozen(after), true);
    assert.equal(Object.isFrozen(after[0]), true);
    assert.equal(after[0].loader, "icon-svg");
    assert.equal(after[0].src, "/icons/{name}.svg");
    assert.equal(Object.hasOwn(after[0], "url"), false);
    assert.equal(Object.hasOwn(after[0], "regexp"), false);
    assert.throws(() => { after[0].src = "/changed"; }, TypeError);
    assert.deepEqual(before, []);
    const result = resolver.resolve("icon:check");
    assert.equal(result.path, "/icons/check.svg");
    assert.equal(result.url, "https://example.test/app/icons/check.svg");
});

test("resolver resolves absolute URL rules without a logical application path", () => {
    const resolver = new Resolver({ config: { app: { basePath: "/app", baseUrl: "https://example.test/app/" }, xshell: { resolver: {} } } });

    resolver.addDefinition("icon:{name}", { src: "https://cdn.example.test/icons/{name}.svg", loader: "icon-svg", cache: true });

    const result = resolver.resolve("icon:check");
    assert.equal(result.path, null);
    assert.equal(result.url, "https://cdn.example.test/icons/check.svg");
});

test("resolver patterns preserve literal text and capture named placeholders", () => {
    const resolver = new Resolver({ config: { app: { basePath: "/app", baseUrl: "https://example.test/app/" }, xshell: { resolver: {} } } });

    resolver.addDefinition("icon:x-{name}", { src: "/icons/x-{name}.svg" });
    resolver.addDefinition("page:/_assets/demo/{path}.js", { src: "/_assets/demo/{path}.js" });
    resolver.addDefinition("resource:{group}-{name}", { src: "/resources/{group}/{name}.js" });
    resolver.addDefinition("literal:x+?([*|{name}.js", { src: "/literal/{name}.js" });

    assert.equal(resolver.resolve("icon:x-check").path, "/icons/x-check.svg");
    const page = resolver.resolve("page:/_assets/demo/orders/details.js");
    assert.equal(page.definition.src, "/_assets/demo/{path}.js");
    assert.equal(page.path, "/_assets/demo/orders/details.js");
    assert.equal(page.url, "https://example.test/app/_assets/demo/orders/details.js");
    const grouped = resolver.resolve("resource:admin-dashboard-panel");
    assert.equal(grouped.path, "/resources/admin/dashboard-panel.js");
    assert.equal(grouped.url, "https://example.test/app/resources/admin/dashboard-panel.js");
    assert.equal(resolver.has("literal:x+?([*|demo.js"), true);
    assert.equal(resolver.has("literal:xxxdemo.js"), false);
    assert.equal(resolver.registry.find(definition => definition.resource === "page:/_assets/demo/{path}.js").resource, "page:/_assets/demo/{path}.js");
});

test("resolver rejects invalid placeholder grammar", () => {
    const resolver = new Resolver({ config: { app: { basePath: "/app", baseUrl: "https://example.test/app/" }, xshell: { resolver: {} } } });
    const invalidPatterns = [
        ["icon:{}", "empty placeholder name"],
        ["icon:{bad-name}", "invalid placeholder name 'bad-name'"],
        ["icon:{name", "unmatched '{'"],
        ["icon:name}", "unmatched '}'"],
        ["icon:{a}{b}", "adjacent placeholders"],
        ["icon:{name}-{name}", "duplicate placeholder 'name'"]
    ];

    for (const [pattern, reason] of invalidPatterns) {
        assert.throws(() => resolver.addDefinition(pattern, { src: "/resource.js" }), new RegExp(reason));
    }
});

test("navigation and areas keep their domain-specific inspection APIs", () => {
    assert.equal("registry" in Navigation.prototype, false);
    assert.equal("registry" in Areas.prototype, false);
});
