import assert from "node:assert/strict";
import test from "node:test";
import { areDeclarativeValuesEqual, createStateSkeleton } from "../contract-state.js";

function createDefinition(state) {
    return { meta: { name: "x-test" }, state };
}

function createContract(property) {
    return { properties: { value: property } };
}

test("compares declarative primitive values by value", () => {
    assert.equal(areDeclarativeValuesEqual(null, null), true);
    assert.equal(areDeclarativeValuesEqual(true, true), true);
    assert.equal(areDeclarativeValuesEqual(12, 12), true);
    assert.equal(areDeclarativeValuesEqual("value", "value"), true);
    assert.equal(areDeclarativeValuesEqual("12", 12), false);
});

test("accepts an equal state-backed primitive default and uses the contract default", () => {
    const contractDefault = "contract";
    const state = createStateSkeleton("test.js", createDefinition({ value: "contract" }), createContract({ state: true, default: contractDefault }), "Component");

    assert.equal(state.value, contractDefault);
});

test("rejects a different state-backed primitive default", () => {
    assert.throws(
        () => createStateSkeleton("test.js", createDefinition({ value: "definition" }), createContract({ state: true, default: "contract" }), "Component"),
        /different defaults.*value/
    );
});

test("accepts equivalent state-backed array defaults", () => {
    const contractDefault = ["one", { two: 2 }];
    const state = createStateSkeleton("test.js", createDefinition({ value: ["one", { two: 2 }] }), createContract({ state: true, default: contractDefault }), "Component");

    assert.equal(state.value, contractDefault);
});

test("rejects different state-backed array defaults", () => {
    assert.throws(
        () => createStateSkeleton("test.js", createDefinition({ value: ["one", "three"] }), createContract({ state: true, default: ["one", "two"] }), "Component"),
        /different defaults.*value/
    );
});

test("accepts equivalent state-backed object defaults", () => {
    const contractDefault = { enabled: true, nested: { count: 2 } };
    const state = createStateSkeleton("test.js", createDefinition({ value: { nested: { count: 2 }, enabled: true } }), createContract({ state: true, default: contractDefault }), "Component");

    assert.equal(state.value, contractDefault);
});

test("rejects different state-backed object defaults", () => {
    assert.throws(
        () => createStateSkeleton("test.js", createDefinition({ value: { enabled: false } }), createContract({ state: true, default: { enabled: true } }), "Component"),
        /different defaults.*value/
    );
});

test("rejects a public property in definition state when it is not state-backed", () => {
    assert.throws(
        () => createStateSkeleton("test.js", createDefinition({ value: "contract" }), createContract({ state: false, default: "contract" }), "Component"),
        /not state-backed/
    );
});

test("accepts private state with no matching public property", () => {
    const state = createStateSkeleton("test.js", createDefinition({ privateValue: null }), { properties: {} }, "Component");

    assert.deepEqual(state, { privateValue: null });
});
