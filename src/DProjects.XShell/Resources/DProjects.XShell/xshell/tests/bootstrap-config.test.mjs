import assert from "node:assert/strict";
import test from "node:test";

import Modules from "../modules.js";
import { discoverModuleConfigs, getLocalModule, mergeConfigs } from "../bootstrap-config.js";

const rootUrl = "https://example.test/modules/app/module.jsonc";
const definition = value => ({ label: value, value });
const reference = configUrl => ({ configUrl });
const loadFrom = (configs, calls = []) => async url => {
    calls.push(url);
    const config = configs[url];
    if (!config) throw new Error(`Missing test config: ${url}`);
    return structuredClone(config);
};

test("getLocalModule identifies the owner structurally instead of by property order", () => {
    const config = { modules: { x: reference("url:../x/module.jsonc"), app: definition("app") } };
    assert.deepEqual(getLocalModule(config, rootUrl), { id: "app", definition: config.modules.app });
});

test("getLocalModule rejects configurations without a local definition", () => {
    const config = { modules: { x: reference("url:../x/module.jsonc"), y: reference("url:../y/module.jsonc") } };
    assert.throws(() => getLocalModule(config, rootUrl), /must contain exactly one local module definition, but found 0/);
});

test("getLocalModule rejects configurations with multiple local definitions", () => {
    const config = { modules: { a: definition("a"), b: definition("b") } };
    assert.throws(() => getLocalModule(config, rootUrl), /found 2: 'a', 'b'/);
});

test("discovery rejects a referenced configuration whose local module id does not match", async () => {
    const dependencyUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(dependencyUrl) } };
    const configs = { [dependencyUrl]: { modules: { y: definition("y") } } };
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom(configs)), /Module reference 'x'.*defines local module 'y'/);
});

test("shared dependencies are fetched once and remain one effective module and runtime instance", async () => {
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
    const graph = await discoverModuleConfigs(root, rootUrl, loadFrom(configs, calls));
    assert.equal(calls.filter(url => url === xUrl).length, 1);
    assert.deepEqual(graph.mergeOrder.map(node => node.id), ["x", "a", "b", "app"]);
    const effective = mergeConfigs(graph.mergeOrder.map(node => node.config));
    assert.deepEqual(Object.keys(effective.modules).sort(), ["a", "app", "b", "x"]);

    // verify the effective map creates one runtime record for the canonical id
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

test("discovery rejects conflicting URLs for the same module id", async () => {
    const aUrl = "https://example.test/modules/a/module.jsonc";
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const x1Url = "https://example.test/modules/x-v1/module.jsonc";
    const x2Url = "https://example.test/modules/x-v2/module.jsonc";
    const root = { modules: { app: definition("app"), a: reference(aUrl), b: reference(bUrl) } };
    const configs = {
        [aUrl]: { modules: { a: definition("a"), x: reference(x1Url) } },
        [bUrl]: { modules: { b: definition("b"), x: reference(x2Url) } }
    };
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom(configs)), /Module 'x' is referenced with conflicting configUrl values/);
});

test("discovery rejects the same URL referenced under the wrong id", async () => {
    const sharedUrl = "https://example.test/modules/shared/module.jsonc";
    const root = { modules: { app: definition("app"), x: reference(sharedUrl), y: reference(sharedUrl) } };
    const configs = { [sharedUrl]: { modules: { x: definition("x") } } };
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom(configs)), /Module reference 'y'.*defines local module 'x'/);
});

test("root composition params and overrides win after dependency-first merging", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: { configUrl: xUrl, label: "root override", params: { mode: "compact" } } } };
    const configs = { [xUrl]: { modules: { x: { ...definition("dependency"), params: { mode: "normal", retained: true } } } } };
    const graph = await discoverModuleConfigs(root, rootUrl, loadFrom(configs));
    assert.deepEqual(graph.mergeOrder.map(node => node.id), ["x", "app"]);
    const effective = mergeConfigs(graph.mergeOrder.map(node => node.config));
    assert.equal(effective.modules.x.label, "root override");
    assert.deepEqual(effective.modules.x.params, { mode: "compact", retained: true });
});

test("child modules cannot configure dependency params", async () => {
    const ordersUrl = "https://example.test/modules/orders/module.jsonc";
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), orders: reference(ordersUrl) } };
    const configs = { [ordersUrl]: { modules: { orders: definition("orders"), x: { configUrl: xUrl, params: { mode: "compact" } } } } };
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom(configs)), /Module 'orders' cannot configure params for dependency 'x'/);
});

test("module references cannot override the referenced definition's assetsUrl", async () => {
    const xUrl = "https://example.test/modules/x/module.jsonc";
    const root = { modules: { app: definition("app"), x: { configUrl: xUrl, assetsUrl: "https://cdn.test/x/" } } };
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom({})), /cannot override assetsUrl; physical assets are owned/);
});

test("dependency cycles are rejected after finite discovery", async () => {
    const bUrl = "https://example.test/modules/b/module.jsonc";
    const root = { modules: { a: definition("a"), b: reference(bUrl) } };
    const configs = { [bUrl]: { modules: { b: definition("b"), a: reference(rootUrl) } } };
    const calls = [];
    await assert.rejects(() => discoverModuleConfigs(root, rootUrl, loadFrom(configs, calls)), /Module dependency cycle detected: 'a' -> 'b' -> 'a'/);
    assert.deepEqual(calls, [bUrl]);
});
