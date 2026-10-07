import assert from "node:assert/strict";
import test from "node:test";

import Areas from "../areas.js";

for (const [rootIndex, runtimeIndex] of [["index.js", "index.js"], ["index.html", "index.js"], ["index.md", "index.md"]]) {
test(`inventory menu normalizes root ${rootIndex} and nested Page destinations`, () => {
    const paths = [
        `/_assets/x-demo/pages/${rootIndex}`,
        "/_assets/x-demo/pages/01-guides/index.html",
        "/_assets/x-demo/pages/01-guides/01-start.js",
        "/_assets/x-demo/pages/01-guides/02-readme.md"
    ];
    const module = { id: "x-demo", config: { menus: { navigation: "/_assets/x-demo/pages" } } };
    const config = {
        xshell: { assetsBasePath: "/_assets", areas: { default: "demo", global: [], definitions: {
            demo: { prefix: "/demo", modules: ["x-demo"] }
        } } },
        modules: { "x-demo": { label: "Demo", files: paths.map(path => ({ path })) } }
    };
    const areas = new Areas({ config, bus: { addEventListener() {} } });
    areas.init({ modules: { getModuleById() { return module; } } });
    const root = areas.getMenu("navigation")[0];
    assert.equal(root.href, `/demo/_assets/x-demo/pages/${runtimeIndex}`);
    assert.equal(root.children[0].href, "/demo/_assets/x-demo/pages/01-guides/index.js");
    assert.equal(root.children[0].children[0].href, "/demo/_assets/x-demo/pages/01-guides/01-start.js");
    assert.equal(root.children[0].children[1].href, "/demo/_assets/x-demo/pages/01-guides/02-readme.md");
});
}
