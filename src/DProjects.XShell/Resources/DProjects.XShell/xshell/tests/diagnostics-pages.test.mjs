import assert from "node:assert/strict";
import test from "node:test";

import BusPage from "../../xshell-diagnostics/pages/bus.js";
import ModulesPage from "../../xshell-diagnostics/pages/modules.js";
import ServicesPage from "../../xshell-diagnostics/pages/services.js";

function createEvents(getController) {
    return {
        on(target, type, listener) {
            target.addEventListener(type, typeof listener === "string" ? () => getController()[listener]() : listener);
        }
    };
}

test("Modules diagnostics filters on the canonical configured assets path", () => {
    const state = { id: "", version: "", label: "", assetsPath: "/_assets/orders", configUrl: "", modules: [] };
    const modules = { registry: [{
        id: "orders", label: "Orders", config: {
            assetsPath: "/_assets/orders", configUrl: "/orders/module.jsonc", files: []
        }
    }] };
    const controller = ModulesPage.controller({ state, modules });

    controller.refresh();
    assert.equal(state.modules.length, 1);
    assert.equal(state.modules[0].assetsPath, "/_assets/orders");
    state.assetsPath = "/_assets/other";
    controller.refresh();
    assert.deepEqual(state.modules, []);
});

test("Bus diagnostics retains only the newest 1000 events", () => {
    const state = Object.assign(new EventTarget(), { registry: [], query_type: "", query_ts: "", query_detail: "" });
    const bus = new EventTarget();
    const page = { invalidate() {} };
    let controller;
    controller = BusPage.controller({ state, bus, page, events: createEvents(() => controller) });
    controller.load();

    for (let id = 0; id < 1002; id++) {
        bus.dispatchEvent(Object.assign(new Event("*"), { ts: id, detail: { id } }));
    }
    assert.equal(state.registry.length, 1000);
    assert.equal(state.registry[0].detail.id, 2);
    assert.equal(state.registry[999].detail.id, 1001);
});

test("Services diagnostics re-reads the public registry when a service is created", () => {
    const state = Object.assign(new EventTarget(), { id: "", moduleId: "", contractId: "", description: "", items: [] });
    const bus = new EventTarget();
    let reads = 0;
    let serviceState = "registered";
    const services = {
        get registry() {
            reads++;
            return [{ id: "example", state: serviceState }];
        }
    };
    let controller;
    controller = ServicesPage.controller({ state, bus, services, events: createEvents(() => controller) });
    controller.load();
    assert.equal(state.items[0].status, "registered");

    serviceState = "created";
    bus.dispatchEvent(new Event("xshell:service:created"));
    assert.equal(reads, 2);
    assert.equal(state.items[0].status, "created");
});
