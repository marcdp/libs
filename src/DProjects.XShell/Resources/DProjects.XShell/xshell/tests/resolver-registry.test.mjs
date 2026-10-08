import assert from "node:assert/strict";
import test from "node:test";

import Resolver from "../resolver.js";
import Navigation from "../navigation.js";
import Areas from "../areas.js";

test("resolver registry snapshots expose rule metadata without mutable definitions", () => {
    const resolver = new Resolver({ config: { app: { basePath: "", baseUrl: "https://example.test/" }, xshell: { resolver: {} } } });
    const before = resolver.registry;
    assert.deepEqual(before, []);

    resolver.addDefinition("icon:{name}", { url: "/icons/{name}.svg", loader: "icon-svg", cache: true });
    const after = resolver.registry;
    assert.equal(Object.isFrozen(after), true);
    assert.equal(Object.isFrozen(after[0]), true);
    assert.equal(after[0].loader, "icon-svg");
    assert.equal(Object.hasOwn(after[0], "regexp"), false);
    assert.throws(() => { after[0].url = "/changed"; }, TypeError);
    assert.deepEqual(before, []);
    const result = resolver.resolve("icon:check");
    assert.equal(result.path, "/icons/check.svg");
    assert.equal(result.url, "https://example.test/icons/check.svg");
});

test("navigation and areas keep their domain-specific inspection APIs", () => {
    assert.equal("registry" in Navigation.prototype, false);
    assert.equal("registry" in Areas.prototype, false);
});
