import assert from "node:assert/strict";
import test from "node:test";

globalThis.HTMLElement = class {};
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.customElements = { get() {}, define() {} };
globalThis.window = { customElements: globalThis.customElements };

const { XTemplateRuntimeUtils } = await import("../render-engines/x.js");

test("XTemplate collection normalization accepts arrays, positive integers, strings and plain objects", () => {
    const collection = XTemplateRuntimeUtils.expr.collection;
    const original = [1, 2];
    assert.equal(collection(original), original);
    for (const value of [null, [], 0, "", {}]) assert.deepEqual(collection(value), [], String(value));
    assert.deepEqual(collection([1, 2]), [1, 2]);
    assert.deepEqual(collection(1), [1]);
    assert.deepEqual(collection(3), [1, 2, 3]);
    assert.deepEqual(collection("a😀"), ["a", "😀"]);
    assert.deepEqual(collection({ a: 1, b: 2 }), ["a", "b"]);
});

test("XTemplate collection normalization rejects unsupported scalar and nonfinite values", () => {
    const collection = XTemplateRuntimeUtils.expr.collection;
    for (const value of [true, false, -1, 1.5, NaN, Infinity, undefined, () => {}]) {
        assert.throws(() => collection(value), undefined, String(value));
    }
});
