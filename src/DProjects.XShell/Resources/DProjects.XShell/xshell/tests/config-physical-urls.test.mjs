import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";

const bootstrapSource = readFileSync(new URL("../bootstrap.js", import.meta.url), "utf8").replace(
    /\/\/ exec bootstrap\s*bootstrap\(\);\s*$/,
    "globalThis.__configUrls = { relativizePaths, prepareModuleConfig };"
);
const context = vm.createContext({
    URL,
    document: {
        location: { origin: "https://example.test" },
        currentScript: { src: "https://example.test/xshell/bootstrap.js" },
        head: {
            querySelector(selector) {
                return { content: selector.includes("app.basePath") ? "/console" : "" };
            }
        }
    }
});
new vm.Script(bootstrapSource).runInContext(context);
const { relativizePaths, prepareModuleConfig } = context.__configUrls;

test("configuration url: references use the physical declaring document", () => {
    const physical = "https://cdn.test/config/app/pages/details.jsonc";
    assert.equal(relativizePaths("url:./theme.css", "/_assets/app", "/pages/details.jsonc", physical),
        "https://cdn.test/config/app/pages/theme.css");
    assert.equal(relativizePaths("url:../shared.css", "/_assets/app", "/pages/details.jsonc", physical),
        "https://cdn.test/config/app/shared.css");
    assert.equal(relativizePaths("./theme.css", "/_assets/app", "/pages/details.jsonc", physical),
        "/_assets/app/pages/theme.css");
    assert.equal(relativizePaths("app:/images/logo.png", "/_assets/app", "/pages/details.jsonc", physical),
        "https://example.test/console/images/logo.png");
});

test("configuration configUrl and assetsUrl retain physical url: semantics", () => {
    const config = {
        modules: {
            app: { assetsUrl: "url:./assets/" },
            other: { configUrl: "url:../other/module.jsonc" }
        }
    };
    const prepared = prepareModuleConfig(config, "https://cdn.test/config/app/module.jsonc", "/_assets");

    assert.equal(prepared.config.modules.app.assetsUrl, "https://cdn.test/config/app/assets/");
    assert.equal(prepared.references[0].configUrl, "https://cdn.test/config/other/module.jsonc");
    assert.equal(prepared.references[0].loadUrl, "https://cdn.test/config/other/module.jsonc");
});
