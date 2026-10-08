import assert from "node:assert/strict";
import test from "node:test";

import ConfigSchema from "../schemas/config.schema.json" with { type: "json" };
import { Validator } from "../vendor/json-schema/4.1.1/json-schema.js";

const validator = new Validator(ConfigSchema.$defs.resolverEntry, "2020-12");

test("resolver schema accepts the supported cache modes", () => {
    const defaultResult = validator.validate({ src: "/resource", loader: "test", cache: true });
    assert.equal(defaultResult.valid, true, JSON.stringify(defaultResult.errors));
    for (const cacheMode of ["full", "path"]) {
        const result = validator.validate({ src: "/resource", loader: "test", cache: true, cacheMode });
        assert.equal(result.valid, true, JSON.stringify(result.errors));
    }
});

test("resolver schema rejects unsupported cache modes", () => {
    const result = validator.validate({ src: "/resource", loader: "test", cache: true, cacheMode: "whatever" });

    assert.equal(result.valid, false);
    assert.match(JSON.stringify(result.errors), /cacheMode|whatever|enum/);
});

test("resolver schema requires src and rejects the former url field", () => {
    const valid = validator.validate({ src: "/_assets/x/file.js", loader: "test", cache: true });
    const invalid = validator.validate({ url: "/_assets/x/file.js", loader: "test", cache: true });

    assert.equal(valid.valid, true, JSON.stringify(valid.errors));
    assert.equal(invalid.valid, false);
});
