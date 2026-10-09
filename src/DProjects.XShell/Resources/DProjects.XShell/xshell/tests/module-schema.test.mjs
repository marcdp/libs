import assert from "node:assert/strict";
import test from "node:test";

import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js";
import validateConfig from "../validation/config.js";

const validator = new Validator(ConfigSchema, "2020-12");

const moduleDefinition = routes => ({
    label: "Test module",
    version: "1.0.0",
    copyright: "",
    icon: "",
    configUrl: "https://example.test/test/module.jsonc",
    assetsUrl: "https://example.test/test/",
    assetsPath: "/_assets/test",
    files: [{ path: "/_assets/test/pages/index.js", size: 123, hash: "test-hash" }],
    ...(routes === undefined ? {} : { routes }),
    defaults: {
        page: { renderEngine: "x", stateEngine: "proxy" },
        component: { renderEngine: "x", stateEngine: "proxy" }
    }
});

function configuration(module) {
    return {
        app: {
            id: "test",
            label: "Test",
            version: "1.0.0",
            copyright: "",
            icon: "",
            basePath: "/",
            baseUrl: "https://example.test/",
            params: {}
        },
        modules: { test: module },
        xshell: {
            build: "test",
            debug: false,
            version: "1.0.0",
            environment: "test",
            assetsBasePath: "/_assets",
            assetsPath: "/_assets/xshell",
            areas: { default: null, global: [], definitions: {} },
            navigation: { mode: "path" },
            resolver: {},
            ui: { layout: {}, component: {}, dialog: {} },
            i18n: {
                lang: "en",
                langs: [{ id: "en", label: "English", main: true }],
                datetime: { options: {}, formats: {} },
                strings: {}
            },
            configUrl: "https://example.test/xshell/xshell.json",
            assetsUrl: "https://example.test/xshell/",
            files: [{ path: "/_assets/xshell/xshell.js", size: 456, hash: "xshell-hash" }]
        }
    };
}

function assertValid(module) {
    const result = validator.validate(configuration(module));
    assert.equal(result.valid, true, JSON.stringify(result.errors));
}

test("module schema keeps routes optional", () => {
    assertValid(moduleDefinition());
});

test("effective schema enforces the lexical module ID grammar", () => {
    for (const moduleId of ["x", "app", "orders", "x-demo", "orders-v2", "xshell-docs"]) {
        const config = configuration(moduleDefinition());
        config.modules = { [moduleId]: moduleDefinition() };
        assert.equal(validator.validate(config).valid, true, moduleId);
    }

    for (const moduleId of ["", "XDemo", "123", "x_demo", "x.demo", "x/demo", "x demo", "-x-demo", "x-demo-", "x--demo", "café", "foo%20bar"]) {
        const config = configuration(moduleDefinition());
        config.modules = { [moduleId]: moduleDefinition() };
        assert.equal(validator.validate(config).valid, false, moduleId);
    }
});

test("effective config rejects unknown Area modules and accepts canonical ids", async () => {
    const config = configuration(moduleDefinition());
    config.xshell.areas.definitions.admin = { modules: ["missing"] };
    await assert.rejects(validateConfig("test", config), /Area 'admin' references unknown module 'missing'/);

    config.xshell.areas.definitions.admin.modules = ["test"];
    await assert.doesNotReject(validateConfig("test", config));
});

test("effective config validates route grammar before runtime construction", async () => {
    const config = configuration(moduleDefinition({ "/repository/{123id}": "/_assets/test/pages/index.js" }));
    await assert.rejects(validateConfig("test", config), /Module 'test' declares invalid route '\/repository\/\{123id\}'/);

    config.modules.test.routes = { "/repository/{repositoryId}/items": "/_assets/test/pages/index.js" };
    await assert.doesNotReject(validateConfig("test", config));
});

test("global menus require arrays while ordinary named menu sources remain valid", async () => {
    const config = configuration(moduleDefinition());
    config.xshell.areas.global = ["shortcuts"];
    config.modules.test.menus = { shortcuts: "runtime-shortcuts", tools: "runtime-tools" };
    await assert.rejects(validateConfig("test", config), /Global menu 'shortcuts' from module 'test' must be a static array/);

    config.modules.test.menus.shortcuts = [{ label: "Shortcut", href: "/pages/shortcut.js" }];
    await assert.doesNotReject(validateConfig("test", config));

    config.xshell.areas.global = [];
    config.modules.test.menus.shortcuts = "runtime-shortcuts";
    await assert.doesNotReject(validateConfig("test", config));
});

test("menu schema accepts embedded and rejects the pre-V0 embeded spelling", () => {
    const module = moduleDefinition();
    module.menus = { navigation: [{ label: "Inline report", href: "/pages/report.js", embedded: true }] };
    assertValid(module);

    module.menus.navigation[0] = { label: "Inline report", href: "/pages/report.js", embeded: true };
    assert.equal(validator.validate(configuration(module)).valid, false);
});

test("effective schema requires app baseUrl", () => {
    const config = configuration(moduleDefinition());
    delete config.app.baseUrl;
    assert.equal(validator.validate(config).valid, false);

    config.app.baseUrl = "https://example.test/";
    assert.equal(validator.validate(config).valid, true, JSON.stringify(validator.validate(config).errors));
});

test("effective schema accepts descriptive module events", () => {
    const module = moduleDefinition();
    module.contract = { events: { ready: { description: "Module is ready" } } };

    assertValid(module);
});

test("effective schema rejects module actions and intents", () => {
    for (const field of ["actions", "intents"]) {
        const module = moduleDefinition();
        module.contract = { events: {}, [field]: {} };

        assert.equal(validator.validate(configuration(module)).valid, false, `${field} must not be part of the V0 module contract`);
    }
});

test("effective schema rejects the removed xshell.identity configuration", () => {
    const config = configuration(moduleDefinition());
    config.xshell.identity = { provider: "anonymous" };
    assert.equal(validator.validate(config).valid, false);
});

test("effective schema requires normalized assetsBasePath and rejects the removed prefix", () => {
    const config = configuration(moduleDefinition());
    delete config.xshell.assetsBasePath;
    assert.equal(validator.validate(config).valid, false);
    config.xshell.assetsBasePath = "app:/_assets";
    assert.equal(validator.validate(config).valid, false);
    config.xshell.assetsBasePath = "/_assets";
    config.xshell.assetsPrefix = "_assets";
    assert.equal(validator.validate(config).valid, false);
});

test("module schema accepts unique non-empty service requirements", () => {
    const module = moduleDefinition();
    module.requires = ["toast", "identity"];

    assertValid(module);
});

test("module schema rejects duplicate or empty service requirements", () => {
    for (const requires of [["toast", "toast"], [""]]) {
        const module = moduleDefinition();
        module.requires = requires;

        assert.equal(validator.validate(configuration(module)).valid, false);
    }
});

test("module schema accepts static and parameterized routes", () => {
    assertValid(moduleDefinition({
        "/something": "/pages/index.js",
        "/repository/{repositoryId}/projects/{projectId}/items": "/pages/index.js"
    }));
});

test("module schema requires route values to be strings", () => {
    const result = validator.validate(configuration(moduleDefinition({ "/something": 123 })));

    assert.equal(result.valid, false);
});

test("module schema rejects object and array route values", () => {
    for (const value of [{ target: "/pages/index.js" }, ["/pages/index.js"]]) {
        const result = validator.validate(configuration(moduleDefinition({ "/something": value })));

        assert.equal(result.valid, false);
    }
});

test("effective schema requires module files", () => {
    const module = moduleDefinition();
    delete module.files;

    assert.equal(validator.validate(configuration(module)).valid, false);
});

test("effective schema requires XShell files", () => {
    const config = configuration(moduleDefinition());
    delete config.xshell.files;

    assert.equal(validator.validate(config).valid, false);
});

test("effective schema requires generated module and XShell assetsPath values", () => {
    const module = moduleDefinition();
    delete module.assetsPath;
    assert.equal(validator.validate(configuration(module)).valid, false);

    const config = configuration(moduleDefinition());
    delete config.xshell.assetsPath;
    assert.equal(validator.validate(config).valid, false);
});

test("effective schema validates inventory path, size, and hash", () => {
    for (const file of [
        { size: 1, hash: "hash" },
        { path: "", size: 1, hash: "hash" },
        { path: "/_assets/test/file.js", size: -1, hash: "hash" },
        { path: "/_assets/test/file.js", size: 1.5, hash: "hash" },
        { path: "/_assets/test/file.js", size: 1, hash: "" }
    ]) {
        const module = moduleDefinition();
        module.files = [file];

        assert.equal(validator.validate(configuration(module)).valid, false);
    }
});

test("effective schema accepts named service contract and implementation objects", () => {
    const config = configuration(moduleDefinition());
    config.xshell.services = {
        toast: {
            contract: "toast",
            implementation: "/_assets/x/services/toast-default.js"
        }
    };

    const result = validator.validate(config);

    assert.equal(result.valid, true, JSON.stringify(result.errors));
});
