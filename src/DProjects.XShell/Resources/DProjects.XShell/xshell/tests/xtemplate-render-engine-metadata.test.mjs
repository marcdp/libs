import assert from "node:assert/strict";
import test from "node:test";

globalThis.HTMLElement = class {};
globalThis.CSSStyleSheet = class { replaceSync() {} };
globalThis.customElements = { get() {}, define() {} };
globalThis.window = { customElements: globalThis.customElements };
let templateCreationCount = 0;
globalThis.document = {
    createElement(tag) {
        if (tag.toLowerCase() === "template") {
            templateCreationCount++;
            throw new Error("Raw XTemplate parsing is forbidden.");
        }
        return {
            tag: tag.toLowerCase(), attrs: {},
            setAttribute(name, value) { this.attrs[name] = value; },
            matches(selector) {
                if (selector === "a" || selector === "img") return this.tag === selector;
                return selector === "input[type=image]" && this.tag === "input" && this.attrs.type === "image";
            }
        };
    }
};

const { default: createRenderEngineFactoryX } = await import("../render-engines/x.js");
const { rewriteTemplateAttribute } = await import("../utils/rewriteDocumentUrls.js");

test("render engine uses frozen compiled metadata and leaves raw XTemplate unparsed", () => {
    const renderer = {
        render: () => [],
        dependencies: [
            { resource: "component:x-lazy", ancestorPaths: [[]] },
            { resource: "component:x-heavy", ancestorPaths: [["x-lazy"]] },
            { resource: "component:x-shared", ancestorPaths: [["x-lazy"], []] }
        ],
        slots: ["", "footer", "footer"]
    };
    const factory = createRenderEngineFactoryX('<div style="display:none"><slot name="raw"></slot><x-raw></x-raw></div>',
        { componentLazy: "x-lazy" }, renderer);
    factory.init();
    assert.deepEqual(factory.dependencies, ["component:x-lazy", "component:x-shared"]);
    assert.deepEqual(factory.slots, ["", "footer"]);
    assert.equal(Object.isFrozen(factory.dependencies), true);
    assert.equal(Object.isFrozen(factory.slots), true);
    assert.equal(templateCreationCount, 0);
});

test("render engine rejects the legacy renderer shape", () => {
    assert.throws(() => createRenderEngineFactoryX("<x-legacy></x-legacy>", {}, () => []),
        /artifact with render, dependencies, and slots/);
});

test("compiled template attributes use navigation and resource URL rules", () => {
    const context = {
        appBasePath: "/app", resourcePath: "/pages/card.js", resourceDefinition: { modulePath: "/module" },
        navigationMode: "hash", navigationHashPrefix: "#"
    };
    assert.equal(rewriteTemplateAttribute("a", { href: "details" }, "href", "details", context), "#/pages/details");
    assert.equal(rewriteTemplateAttribute("img", { src: "images/a.png" }, "src", "images/a.png", context), "/app/pages/images/a.png");
    assert.equal(rewriteTemplateAttribute("input", { type: "image", src: "send.png" }, "src", "send.png", context), "/app/pages/send.png");
});
