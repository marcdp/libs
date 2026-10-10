import assert from "node:assert/strict";
import test from "node:test";

class FakeElement {}
class TestStyleSheet { replaceSync() {} }

globalThis.HTMLElement = FakeElement;
globalThis.CSSStyleSheet = TestStyleSheet;
globalThis.customElements = { define() {}, get() { return undefined; } };
globalThis.window = { customElements: globalThis.customElements };
globalThis.document = { adoptedStyleSheets: [], baseURI: "https://example.test/" };

const { XTemplateRuntimeUtils } = await import("../render-engines/x.js");
const transform = (value, name, ...args) => XTemplateRuntimeUtils.expr.transform(value, name, () => args, null);

test("slug follows the Unicode conformance cases", () => {
    const cases = [
        ["Hello World", "hello-world"], ["  Hello, World!  ", "hello-world"], ["Crème brûlée", "creme-brulee"],
        ["España", "espana"], ["Über uns", "uber-uns"], ["foo___bar", "foo-bar"],
        ["Foo / Bar / Baz", "foo-bar-baz"], ["東京 News", "東京-news"], ["C++", "c"], ["C#", "c"],
        ["", ""], ["---", ""], ["e\u0301", "e"], ["ﬁ Title", "fi-title"], ["𠀀 News", "𠀀-news"]
    ];
    for (const [input, expected] of cases) assert.equal(transform(input, "slug"), expected, input);
});

test("slug propagates null and rejects arguments and non-string input", () => {
    assert.equal(transform(null, "slug"), null);
    assert.equal(XTemplateRuntimeUtils.expr.transform(null, "slug", () => { throw new Error("arguments must stay lazy"); }, null), null);
    assert.throws(() => transform("title", "slug", 1), /Transformer 'slug' received an invalid argument count/);
    assert.throws(() => transform(123, "slug"), /Transformer 'slug' requires a string input/);
});

test("slug is invariant across configured locales", () => {
    for (const locale of ["en-US", "es-ES", "tr-TR"]) {
        assert.equal(XTemplateRuntimeUtils.expr.transform("İstanbul", "slug", () => [], { locale }), "istanbul");
    }
});
