import assert from "node:assert/strict";
import test from "node:test";

import objectDefinition, { contract } from "../../x/components/x-object.js";

function inspect(value, inspection = null) {
    const state = { ...objectDefinition.state, value, expanded: false };
    const host = {};
    const controller = objectDefinition.controller({ state, host, events: { on() {} } });
    host._inspection = inspection;
    controller.load();
    return { state, controller };
}

function inspectEntry(entry) {
    return inspect(entry.value, entry.inspection);
}

test("x-object retains primitive formatting and handles invalid Dates", () => {
    const values = [
        ["hello", "\"hello\"", "string"],
        [12, "12", "number"],
        [true, "true", "boolean"],
        [12n, "12", "bigint"],
        [null, "null", "null"],
        [undefined, "undefined", "undefined"],
        [new Date("2024-01-02T03:04:05.000Z"), "2024-01-02T03:04:05.000Z", "date"],
        [function example() {}, "ƒ example()", "function"],
        [Symbol("value"), "Symbol(value)", "symbol"]
    ];
    for (const [value, text, type] of values) {
        const { state } = inspect(value);
        assert.equal(state.text, text);
        assert.equal(state.type, type);
        assert.equal(state.expandable, false);
    }
    const invalid = inspect(new Date(NaN)).state;
    assert.equal(invalid.text, "Invalid Date");
    assert.equal(invalid.expandable, false);
    const functionWithGetter = function() {};
    Object.defineProperty(functionWithGetter, "name", { get() { throw new Error("name getter must not execute"); } });
    assert.equal(inspect(functionWithGetter).state.text, "ƒ ()");
});

test("x-object expands only collections with enumerable entries", () => {
    for (const [value, text] of [[{}, "{}"], [[], "[]"]]) {
        const { state, controller } = inspect(value);
        assert.equal(state.expandable, false);
        assert.equal(state.text, text);
        controller.expand();
        assert.equal(state.expanded, false);
    }
    for (const value of [{ a: 1 }, [1]]) {
        const { state, controller } = inspect(value);
        assert.equal(state.expandable, true);
        controller.expand();
        assert.equal(state.expanded, true);
        controller.toggle();
        assert.equal(state.expanded, false);
        controller.collapse();
        assert.equal(state.expanded, false);
    }
    class Example {}
    assert.equal(inspect(new Example()).state.text, "Example {}");
    Object.defineProperty(Example, "name", { get() { throw new Error("constructor name getter must not execute"); } });
    assert.equal(inspect(new Example()).state.text, "{}");
    assert.equal(inspect({ items: [1] }).state.summary, "{items: Array(1)}");
    const fn = () => { throw new Error("function must not execute"); };
    const functionEntry = inspect({ fn }).state.entries[0];
    assert.equal(functionEntry.renderValue(), fn);
    assert.equal(inspectEntry(functionEntry).state.text, "ƒ fn()");
});

test("x-object stops cycles on the current branch and allows shared objects in separate branches", () => {
    const self = {};
    self.self = self;
    const selfRoot = inspect(self).state;
    assert.equal(selfRoot.summary, "{self: [Circular]}");
    const selfChild = inspectEntry(selfRoot.entries[0]).state;
    assert.equal(selfChild.text, "[Circular]");
    assert.equal(selfChild.expandable, false);
    assert.deepEqual(selfChild.entries, []);

    const parent = {};
    const child = { parent };
    parent.child = child;
    const childState = inspectEntry(inspect(parent).state.entries[0]).state;
    assert.equal(childState.summary, "{parent: [Circular]}");
    const cycleState = inspectEntry(childState.entries[0]).state;
    assert.equal(cycleState.text, "[Circular]");
    assert.equal(cycleState.expandable, false);

    const shared = { id: 1 };
    const root = inspect({ left: shared, right: shared }).state;
    for (const entry of root.entries) {
        const branch = inspectEntry(entry).state;
        assert.equal(branch.expandable, true);
        assert.equal(branch.summary, "{id: 1}");
    }
});

test("x-object describes accessors without invoking them in rows or summaries", () => {
    const value = {};
    Object.defineProperty(value, "dangerous", { enumerable: true, get() { throw new Error("getter must not execute"); } });
    Object.defineProperty(value, "writeOnly", { enumerable: true, set(_) {} });
    Object.defineProperty(value, "both", { enumerable: true, get() { throw new Error("getter must not execute"); }, set(_) {} });
    const root = inspect(value).state;
    assert.equal(root.summary, "{dangerous: [Getter], writeOnly: [Setter], both: [Getter/Setter]}");
    assert.deepEqual(root.entries.map(entry => inspectEntry(entry).state.text), ["[Getter]", "[Setter]", "[Getter/Setter]"]);
    assert.ok(root.entries.every(entry => !inspectEntry(entry).state.expandable));
});

test("x-object uses a native disclosure button with reactive aria-expanded", () => {
    assert.equal(Object.hasOwn(contract.properties, "_inspection"), false);
    assert.match(objectDefinition.template, /<button\s+x-if="state\.expandable"\s+type="button"[\s\S]*?x-attr:aria-expanded="state\.expanded \? 'true' : 'false'"[\s\S]*?x-on:click="toggle"/);
    assert.match(objectDefinition.template, /<div\s+x-else\s+class="node">/);
    const { state, controller } = inspect({ answer: 42 });
    state.expanded = false;
    controller.toggle();
    assert.equal(state.expanded, true);
    controller.toggle();
    assert.equal(state.expanded, false);
});
