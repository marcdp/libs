import assert from "node:assert/strict";
import test from "node:test";

import menuDefinition from "../../x/components/x-menu.js";
import menuItemDefinition, { contract as menuItemContract } from "../../x/components/x-menuitem.js";

test("x-menu passes canonical tooltip and embedded properties to x-menuitem", () => {
    assert.match(menuDefinition.template, /x-attr:tooltip="menuitem\.tooltip"/);
    assert.match(menuDefinition.template, /x-attr:embedded="menuitem\.embedded"/);
});

test("x-menuitem exposes tooltip and embedded as public attributes", () => {
    assert.deepEqual(menuItemContract.properties.tooltip, {
        type: "string",
        default: "",
        attribute: true,
        state: true,
        description: "Optional tooltip text shown on the navigation anchor."
    });
    assert.deepEqual(menuItemContract.properties.embedded, {
        type: "boolean",
        default: false,
        attribute: true,
        state: true,
        description: "Hosts the target Page inline instead of rendering a navigation anchor."
    });
});

test("x-menuitem renders tooltip on a normal anchor and an embedded target as an inline Page", () => {
    assert.match(
        menuItemDefinition.template,
        /<div x-elseif="state\.embedded">\s*<x-page x-attr:src="state\.href" loading="lazy"><\/x-page>\s*<\/div>/
    );
    assert.match(menuItemDefinition.template, /<x-anchor x-else[^>]*x-attr:title="state\.tooltip"/);
});
