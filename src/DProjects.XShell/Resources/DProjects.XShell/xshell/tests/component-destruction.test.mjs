import assert from "node:assert/strict";
import test from "node:test";
import { unloadComponents, unloadChildComponents } from "../utils/components.js";

class ComponentNode {
    static get isXShellComponent() { return true; }

    childNodes = [];
    unloadCount = 0;

    appendChild(child) {
        this.childNodes.push(child);
        return child;
    }

    unload() {
        if (this.unloadCount) return;
        this.unloadCount++;
    }
}

class PlainNode {
    childNodes = [];

    appendChild(child) {
        this.childNodes.push(child);
        return child;
    }
}

test("unloadComponents finalizes marked Components in light-DOM subtrees", () => {
    const root = new PlainNode();
    const parent = root.appendChild(new ComponentNode());
    const nested = parent.appendChild(new ComponentNode());
    const plain = root.appendChild(new PlainNode());
    const sibling = plain.appendChild(new ComponentNode());

    unloadComponents(root);

    assert.equal(parent.unloadCount, 1);
    assert.equal(nested.unloadCount, 1);
    assert.equal(sibling.unloadCount, 1);
});

test("unloadComponents ignores unrelated unload methods", () => {
    let calls = 0;
    const root = { childNodes: [], unload() { calls++; } };

    unloadComponents(root);

    assert.equal(calls, 0);
});

test("unloadChildComponents preserves retained direct children", () => {
    const root = new PlainNode();
    const retained = root.appendChild(new ComponentNode());
    const discarded = root.appendChild(new ComponentNode());

    unloadChildComponents(root, [retained]);

    assert.equal(retained.unloadCount, 0);
    assert.equal(discarded.unloadCount, 1);
});
