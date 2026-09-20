import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import Areas from "../../src/DProjects.XShell/Resources/DProjects.XShell/xshell/areas.js";
import Navigation from "../../src/DProjects.XShell/Resources/DProjects.XShell/xshell/navigation.js";

const root = new URL("../../src/DProjects.XShell/Resources/DProjects.XShell/modules/test/module.jsonc", import.meta.url);
const moduleConfig = JSON.parse(readFileSync(root, "utf8").replace(/\/\/[^\r\n]*/g, ""));

function createAreas(config = moduleConfig) {
    const listeners = new Map();
    const events = [];
    const bus = {
        addEventListener(name, listener) {
            const registered = listeners.get(name) || [];
            registered.push(listener);
            listeners.set(name, registered);
        },
        emit(name, detail) {
            events.push({ name, detail });
            for (const listener of listeners.get(name) || []) listener({ detail });
        }
    };
    // bootstrap has already normalized module-relative resource hrefs
    const module = {
        id: "test",
        config: {
            ...config.modules.test,
            menus: Object.fromEntries(Object.entries(config.modules.test.menus).map(([name, items]) => [
                name,
                items.map(item => normalizeItem(item))
            ]))
        }
    };
    const modules = { getModuleById: id => id === "test" ? module : null };
    const areas = new Areas({ config, bus });
    areas.init({ modules });
    return { areas, bus, events, modules, module };
}

function normalizeItem(item) {
    return {
        ...item,
        href: item.href?.startsWith("/") ? "/_assets/test" + item.href : item.href,
        children: item.children?.map(normalizeItem)
    };
}

test("one canonical module composes independent Area menus and homes", () => {
    const { areas, modules, module } = createAreas();
    const main = areas.getArea("main");
    const advanced = areas.getArea("advanced");
    assert.equal(main.prefix, "");
    assert.equal(advanced.prefix, "/advanced");
    assert.equal(main.home, "/_assets/test/pages/test0.js");
    assert.equal(advanced.home, "/advanced/_assets/test/pages/test0.js");
    assert.notStrictEqual(main.menus.navigation, advanced.menus.navigation);
    assert.notStrictEqual(main.menus.navigation[0], advanced.menus.navigation[0]);
    assert.notStrictEqual(main.menus.navigation[0].children, advanced.menus.navigation[0].children);
    assert.strictEqual(modules.getModuleById("test"), module);
    assert.equal(areas.getMenu("navigation")[0].href, main.home);
    assert.equal(areas.getMenu("navigation", "advanced")[0].href, advanced.home);
    assert.deepEqual(areas.getMenuitemBreadcrumb("/advanced/_assets/test/pages/test1.js", "advanced").map(item => item.label), ["Amazon S3", "Page 1"]);
});

test("Area changes emit only the Area event and use the longest prefix", () => {
    const { areas, bus, events } = createAreas();
    assert.equal(areas.resolveAreaId("/advanced/_assets/test/pages/test0.js"), "advanced");
    bus.emit("xshell:navigation:end", { src: "/advanced/_assets/test/pages/test0.js" });
    assert.equal(areas.getCurrentArea().id, "advanced");
    assert.equal(events.filter(event => event.name === "xshell:area:change").length, 1);
    assert.equal(events.filter(event => event.name === "xshell:menus:changed").length, 0);
});

test("home uses the first top-level default in Area module order", () => {
    const config = {
        xshell: { areas: { default: "main", definitions: { main: { prefix: "", modules: ["first", "second"] } } } }
    };
    const definitions = {
        first: { id: "first", config: { menus: { navigation: [
            { label: "Parent", href: "/_assets/first/parent.js", children: [{ label: "Child", href: "/_assets/first/child.js", default: true }] },
            { label: "First", href: "/_assets/first/home.js", default: true }
        ] } } },
        second: { id: "second", config: { menus: { navigation: [{ label: "Second", href: "/_assets/second/home.js", default: true }] } } }
    };
    const bus = { addEventListener() {}, emit() {} };
    const areas = new Areas({ config, bus });
    areas.init({ modules: { getModuleById: id => definitions[id] } });
    assert.equal(areas.getDefaultArea().home, "/_assets/first/home.js");
    assert.deepEqual(areas.getMenu("navigation").map(item => item.label), ["Parent", "First", "Second"]);
});

test("prefix normalization and longest segment matching", () => {
    const config = { xshell: { areas: { default: "root", definitions: {
        root: { prefix: "/", modules: [] },
        admin: { prefix: "/admin/", modules: [] },
        tools: { prefix: "admin/tools", modules: [] }
    } } } };
    const areas = new Areas({ config, bus: { addEventListener() {}, emit() {} } });
    areas.init({ modules: { getModuleById() { return null; } } });
    assert.equal(areas.getArea("root").prefix, "");
    assert.equal(areas.getArea("admin").prefix, "/admin");
    assert.equal(areas.resolveAreaId("/admin/tools/page"), "tools");
    assert.equal(areas.resolveAreaId("/admin/toolshed"), "admin");
    assert.equal(areas.resolveAreaId("/anything"), "root");
});

test("home has no fallback and external hrefs bypass Area prefixes", () => {
    const config = structuredClone(moduleConfig);
    delete config.modules.test.menus.navigation[0].default;
    config.xshell.areas.definitions.main.home = "/obsolete";
    config.modules.test.menus.tools.push({ label: "External", href: "mailto:team@example.com" });
    const { areas } = createAreas(config);
    assert.equal(areas.getArea("main").home, null);
    assert.equal(areas.getArea("advanced").home, null);
    assert.equal(areas.getMenu("tools", "advanced")[1].href, "mailto:team@example.com");
});

test("hash-mode startup uses the composed default Area home", async () => {
    const { areas, bus } = createAreas();
    const location = { hash: "" };
    const previousWindow = globalThis.window;
    const previousDocument = globalThis.document;
    const previousLocation = globalThis.location;
    globalThis.window = { addEventListener() {} };
    globalThis.document = { location };
    globalThis.location = location;
    try {
        const config = { app: { base: "/" }, xshell: { navigation: { mode: "hash", hashPrefix: "#!" } } };
        const navigation = new Navigation({ areas, bus, config, container: {} });
        await navigation.init();
        assert.equal(location.hash, "#!/_assets/test/pages/test0.js");
    } finally {
        globalThis.window = previousWindow;
        globalThis.document = previousDocument;
        globalThis.location = previousLocation;
    }
});

test("dynamic children refresh every Area copy", () => {
    const config = structuredClone(moduleConfig);
    config.modules.test.menus.navigation[0].childrenSource = "recent";
    const listeners = new Map();
    const events = [];
    const bus = {
        addEventListener: (name, listener) => listeners.set(name, listener),
        emit: (name, detail) => events.push({ name, detail })
    };
    const areas = new Areas({ config, bus });
    let label = "First";
    areas.registerSource("recent", { dependsOn: ["updated"], resolve: () => [{ label, href: "/_assets/test/pages/test2.js" }] });
    areas.init({ modules: { getModuleById: id => id === "test" ? { id, config: config.modules.test } : null } });
    label = "Second";
    listeners.get("updated")();
    assert.equal(areas.getMenu("navigation", "main")[0].children[0].label, "Page 1");
    assert.equal(areas.getMenu("navigation", "main")[0].children.at(-1).label, "Second");
    assert.equal(areas.getMenu("navigation", "advanced")[0].children.at(-1).href, "/advanced/_assets/test/pages/test2.js");
    assert.equal(events.filter(event => event.name === "xshell:menus:changed").length, 1);
    listeners.get("updated")();
    assert.equal(events.filter(event => event.name === "xshell:menus:changed").length, 1);
});
