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

test("Date values are canonical XTemplate scalars", () => {
    assert.equal(XTemplateRuntimeUtils.expr.scalar(new Date("2026-09-29T17:30:00Z")), "2026-09-29T17:30:00.000Z");
    assert.throws(() => XTemplateRuntimeUtils.expr.scalar(new Date("invalid")), /XTemplate runtime error: Invalid date\/time value/);
    assert.throws(() => XTemplateRuntimeUtils.expr.scalar({}), /XTemplate runtime error: Value cannot be converted to an XTemplate scalar/);
    assert.throws(() => XTemplateRuntimeUtils.expr.scalar([]), /XTemplate runtime error: Value cannot be converted to an XTemplate scalar/);
});

test("Date values work with date/time transformers in UTC", () => {
    const value = new Date("2026-09-29T17:30:00Z");

    assert.equal(transform(value, "date", "dd/MM/yyyy"), "29/09/2026");
    assert.equal(transform(value, "datetime", "dd/MM/yyyy HH:mm:ss"), "29/09/2026 17:30:00");
    assert.equal(transform(value, "time", "HH:mm:ss"), "17:30:00");
    assert.equal(transform(null, "date", "dd/MM/yyyy"), null);
    assert.throws(() => transform(new Date("invalid"), "date", "dd/MM/yyyy"), /XTemplate runtime error: Invalid date\/time value/);
});
