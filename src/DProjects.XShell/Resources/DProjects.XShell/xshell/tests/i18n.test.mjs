import assert from "node:assert/strict";
import test from "node:test";

import I18n from "../i18n.js";
import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js";

const validator = new Validator(ConfigSchema, "2020-12");

function i18nConfiguration(overrides = {}) {
    return {
        lang: "en",
        langs: [{ id: "en", label: "English", main: true }],
        datetime: { options: {}, formats: {} },
        strings: {},
        ...overrides
    };
}

function configuration(i18n) {
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
        modules: {},
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
            i18n,
            configUrl: "https://example.test/xshell/xshell.jsonc",
            assetsUrl: "https://example.test/xshell/",
            files: []
        }
    };
}

function validate(i18n) {
    return validator.validate(configuration(i18n));
}

test("i18n schema accepts the effective configuration", () => {
    const result = validate(i18nConfiguration());

    assert.equal(result.valid, true, JSON.stringify(result.errors));
});

test("i18n schema rejects a missing language", () => {
    const i18n = i18nConfiguration();
    delete i18n.lang;

    assert.equal(validate(i18n).valid, false);
});

test("i18n schema rejects invalid language definitions", () => {
    assert.equal(validate(i18nConfiguration({ langs: "en" })).valid, false);
    assert.equal(validate(i18nConfiguration({ langs: [{ label: "English" }] })).valid, false);
});

test("i18n schema rejects non-string translations", () => {
    const result = validate(i18nConfiguration({ strings: { es: { Save: 42 } } }));

    assert.equal(result.valid, false);
});

test("init uses the supplied effective configuration", async () => {
    const config = i18nConfiguration({ lang: "es", strings: { es: { Save: "Guardar" } } });
    const i18n = new I18n();

    await i18n.init(config);

    assert.equal(i18n.config, config);
});

test("translate uses the active language dictionary and falls back to the source label", async () => {
    const i18n = new I18n();
    await i18n.init(i18nConfiguration({ lang: "es", strings: { es: { Save: "Guardar", Empty: "" } } }));

    assert.equal(i18n.translate("Save"), "Guardar");
    assert.equal(i18n.translate("Empty"), "");
    assert.equal(i18n.translate("Unknown"), "Unknown");
});

test("translate falls back when the active language has no dictionary", async () => {
    const i18n = new I18n();
    await i18n.init(i18nConfiguration({ lang: "fr", strings: { es: { Save: "Guardar" } } }));

    assert.equal(i18n.translate("Save"), "Save");
});
