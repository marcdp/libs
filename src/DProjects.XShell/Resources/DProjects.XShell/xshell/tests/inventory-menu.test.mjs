import assert from "node:assert/strict";
import test from "node:test";

import DemoModule from "../../x-demo/js/module.js";

test("x-demo consumes normalized effective inventory paths without prefixing them twice", () => {
    let source;
    new DemoModule({
        areas: {
            registerSource(name, value) {
                assert.equal(name, "x-demo-dynamic-navigation-menu-source");
                source = value;
            }
        },
        moduleAssetsPath: "/_assets/x-demo",
        moduleConfig: {
            files: [
                { path: "/_assets/x-demo/pages/index.js", size: 1, hash: "root" },
                { path: "/_assets/x-demo/pages/01-guides/index.js", size: 2, hash: "section" },
                { path: "/_assets/x-demo/pages/01-guides/01-start.js", size: 3, hash: "page" }
            ]
        }
    });

    const menu = source.resolve();

    assert.equal(menu[0].href, "/_assets/x-demo/pages/index.js");
    assert.equal(menu[0].children[0].href, "/_assets/x-demo/pages/01-guides/index.js");
    assert.equal(menu[0].children[0].children[0].href, "/_assets/x-demo/pages/01-guides/01-start.js");
});
