import assert from "node:assert/strict";
import test from "node:test";

class FakeElement {}
class TestStyleSheet {
    replaceSync() {}
}

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = TestStyleSheet;
globalThis.customElements = { define() {}, get() { return undefined; } };
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = { adoptedStyleSheets: [], baseURI: "https://example.test/" };

const { XTemplateRuntimeUtils } = await import("../render-engines/x.js");
const transform = (value, name, ...args) => XTemplateRuntimeUtils.expr.transform(value, name, () => args, null);

test("json_stringify serializes every JSON-compatible XTemplate value kind", () => {
    assert.equal(transform({ name: "Marc", enabled: true }, "json_stringify"), '{"name":"Marc","enabled":true}');
    assert.equal(transform([1, 2, 3], "json_stringify"), "[1,2,3]");
    assert.equal(transform("abc", "json_stringify"), '"abc"');
    assert.equal(transform(123, "json_stringify"), "123");
    assert.equal(transform(true, "json_stringify"), "true");
    assert.equal(transform(null, "json_stringify"), "null");
    assert.equal(transform({ items: [{ id: 1 }, { id: 2 }] }, "json_stringify"), '{"items":[{"id":1},{"id":2}]}');
});

test("json_stringify rejects arguments and values that cannot be serialized", () => {
    assert.throws(() => transform("value", "json_stringify", 2), /Transformer 'json_stringify' received an invalid argument count/);
    const cyclic = {};
    cyclic.self = cyclic;
    assert.throws(
        () => transform(cyclic, "json_stringify"),
        /XTemplate runtime error: Transformer 'json_stringify' received a value that cannot be serialized/
    );
});

test("json_parse produces normal XTemplate values", () => {
    assert.deepEqual(transform('{"name":"Marc","enabled":true}', "json_parse"), { name: "Marc", enabled: true });
    assert.deepEqual(transform("[1,2,3]", "json_parse"), [1, 2, 3]);
    assert.equal(transform('"abc"', "json_parse"), "abc");
    assert.equal(transform("123", "json_parse"), 123);
    assert.equal(transform("true", "json_parse"), true);
    assert.equal(transform("null", "json_parse"), null);
});

test("json_parse rejects invalid inputs", () => {
    assert.throws(() => transform(123, "json_parse"), /Transformer 'json_parse' requires a string input/);
    assert.throws(() => transform("not json", "json_parse"), /Transformer 'json_parse' received invalid JSON/);
    assert.throws(() => transform("{}", "json_parse", "x"), /Transformer 'json_parse' received an invalid argument count/);
    assert.throws(() => transform("1e400", "json_parse"), /Transformer 'json_parse' received invalid JSON/);
});

test("JSON transformer results support member access, indexing, and round trips", () => {
    const parsedObject = transform('{"name":"Marc","items":[{"id":1}]}', "json_parse");
    assert.equal(XTemplateRuntimeUtils.expr.member(parsedObject, "name"), "Marc");
    const firstItem = XTemplateRuntimeUtils.expr.index(XTemplateRuntimeUtils.expr.member(parsedObject, "items"), 0);
    assert.equal(XTemplateRuntimeUtils.expr.member(firstItem, "id"), 1);
    assert.deepEqual(XTemplateRuntimeUtils.expr.collection(XTemplateRuntimeUtils.expr.member(parsedObject, "items")), [{ id: 1 }]);
    const value = { name: "Marc", enabled: true, items: [1, { id: 2 }] };
    assert.deepEqual(transform(transform(value, "json_stringify"), "json_parse"), value);
    assert.equal(transform(transform('{ "name": "Marc", "enabled": true }', "json_parse"), "json_stringify"), '{"name":"Marc","enabled":true}');
    assert.equal(transform(transform("null", "json_parse"), "json_stringify"), "null");
});

test("existing transformers retain null propagation", () => {
    assert.equal(transform(null, "trim"), null);
    assert.equal(transform(null, "upper"), null);
    assert.equal(transform(null, "number"), null);
    assert.equal(transform(null, "json_parse"), null);
});
