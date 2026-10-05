import assert from "node:assert/strict";
import test from "node:test";

import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js";

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
            params: {}
        },
        modules: { test: module },
        xshell: {
            build: "test",
            debug: false,
            version: "1.0.0",
            environment: "test",
            assetsPrefix: "_assets",
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
            configUrl: "https://example.test/xshell/xshell.jsonc",
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

test("effective schema rejects the removed xshell.identity configuration", () => {
    const config = configuration(moduleDefinition());
    config.xshell.identity = { provider: "anonymous" };
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
