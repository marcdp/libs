import assert from "node:assert/strict";
import test from "node:test";

import listViewDefinition, { contract } from "../../x/components/x-listview.js";

function createListView(view = contract.properties.view.default, views = contract.properties.views.default) {
    const state = { view, views, availableViews: [] };
    const events = { on() {} };
    const host = { shadowRoot: { querySelector() { return null; } } };
    const controller = listViewDefinition.controller({ state, events, host });

    return { controller, state };
}

test("x-listview preserves its default view when no selectable views are configured", async () => {
    const { controller, state } = createListView();

    await controller.load();

    assert.equal(state.view, "list");
    assert.deepEqual(state.availableViews, []);
});

test("x-listview preserves an explicitly selected supported view when no selectable views are configured", () => {
    for (const view of ["list", "details", "tiles", "icons"]) {
        const { controller, state } = createListView(view);

        controller.refreshViews();

        assert.equal(state.view, view);
        assert.deepEqual(state.availableViews, []);
    }
});

test("x-listview preserves a current view included in the configured views", () => {
    const { controller, state } = createListView("details", "list,details,tiles");

    controller.refreshViews();

    assert.equal(state.view, "details");
    assert.deepEqual(state.availableViews, ["list", "details", "tiles"]);
});

test("x-listview selects the first configured view when the current view is not available", () => {
    const { controller, state } = createListView("icons", "list,details");

    controller.refreshViews();

    assert.equal(state.view, "list");
    assert.deepEqual(state.availableViews, ["list", "details"]);
});

test("x-listview normalizes, validates, and deduplicates configured views", () => {
    const { controller, state } = createListView("details", " LIST, details, list, invalid, tiles ");

    controller.refreshViews();

    assert.equal(state.view, "details");
    assert.deepEqual(state.availableViews, ["list", "details", "tiles"]);
});

test("x-listview renders its view selector only when multiple views are available", () => {
    assert.match(listViewDefinition.template, /<x-dropdown x-if="state\.availableViews\.length > 1"/);
});
