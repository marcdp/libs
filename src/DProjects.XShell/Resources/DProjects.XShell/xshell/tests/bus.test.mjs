import assert from "node:assert/strict";
import test from "node:test";

import Bus from "../bus.js";

function createBus(t) {
    const bus = new Bus();
    t.after(() => {
        bus._channel.port1.close();
        bus._channel.port2.close();
    });
    return bus;
}

test("emit returns immediately and delivers to exact and wildcard listeners asynchronously", { timeout: 1000 }, async t => {
    const bus = createBus(t);
    const calls = [];
    let delivered;
    const delivery = new Promise(resolve => { delivered = resolve; });
    bus.addEventListener("orders:changed", event => calls.push(["exact", event]));
    bus.addEventListener("*", event => {
        calls.push(["wildcard", event]);
        delivered();
    });

    const result = bus.emit("orders:changed", { id: 42 });
    assert.equal(result, undefined);
    assert.deepEqual(calls, []);

    await delivery;
    assert.deepEqual(calls.map(([kind]) => kind), ["exact", "wildcard"]);
    assert.equal(calls[0][1], calls[1][1]);
    assert.equal(calls[0][1].type, "orders:changed");
    assert.deepEqual(calls[0][1].detail, { id: 42 });
    assert.equal(typeof calls[0][1].ts, "number");
});

test("throwing exact and wildcard listeners do not stop delivery", { timeout: 1000 }, async t => {
    const bus = createBus(t);
    const errors = [];
    t.mock.method(console, "error", (...args) => errors.push(args));
    const calls = [];
    let delivered;
    const delivery = new Promise(resolve => { delivered = resolve; });
    const exactError = new Error("exact failed");
    const wildcardError = new Error("wildcard failed");
    bus.addEventListener("orders:changed", () => { throw exactError; });
    bus.addEventListener("orders:changed", () => calls.push("exact"));
    bus.addEventListener("*", () => { throw wildcardError; });
    bus.addEventListener("*", () => {
        calls.push("wildcard");
        delivered();
    });

    bus.emit("orders:changed");
    await delivery;

    assert.deepEqual(calls, ["exact", "wildcard"]);
    assert.deepEqual(errors.map(([, error]) => error), [exactError, wildcardError]);
});

test("rejected async listeners are reported without stopping delivery or producing unhandled rejections", { timeout: 1000 }, async t => {
    const bus = createBus(t);
    const errors = [];
    t.mock.method(console, "error", (...args) => errors.push(args));
    const calls = [];
    let delivered;
    const delivery = new Promise(resolve => { delivered = resolve; });
    const rejection = new Error("async failed");
    bus.addEventListener("orders:changed", () => Promise.reject(rejection));
    bus.addEventListener("orders:changed", () => calls.push("exact"));
    bus.addEventListener("*", () => ({ then(resolve, reject) { reject(rejection); } }));
    bus.addEventListener("*", () => {
        calls.push("wildcard");
        delivered();
    });

    bus.emit("orders:changed");
    await delivery;
    await new Promise(resolve => setImmediate(resolve));

    assert.deepEqual(calls, ["exact", "wildcard"]);
    assert.deepEqual(errors.map(([, error]) => error), [rejection, rejection]);
});

test("removeEventListener removes exact and wildcard registrations", { timeout: 1000 }, async t => {
    const bus = createBus(t);
    const calls = [];
    const removedExact = () => calls.push("removed exact");
    const removedWildcard = () => calls.push("removed wildcard");
    let delivered;
    const delivery = new Promise(resolve => { delivered = resolve; });
    bus.addEventListener("orders:changed", removedExact);
    bus.addEventListener("*", removedWildcard);
    bus.addEventListener("*", () => {
        calls.push("remaining wildcard");
        delivered();
    });
    bus.removeEventListener("orders:changed", removedExact);
    bus.removeEventListener("*", removedWildcard);

    bus.emit("orders:changed");
    await delivery;

    assert.deepEqual(calls, ["remaining wildcard"]);
});
