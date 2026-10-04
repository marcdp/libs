import assert from "node:assert/strict";
import test from "node:test";
import definition from "../../x/components/x-markdown.js";

const source = "/_assets/xshell-docs/pages/10-architecture/100-services.md";
globalThis.document = {};
globalThis.DOMParser = class {};

function element(localName, attributes = {}, children = []) {
    return {
        localName,
        attributes: { ...attributes },
        content: { querySelectorAll() { return children; } },
        getAttribute(name) { return this.attributes[name] ?? null; },
        setAttribute(name, value) { this.attributes[name] = value; },
        matches(selector) {
            return selector.split(", ").some(rule => rule === `${localName}[href]` && "href" in this.attributes
                || rule === `${localName}[src]` && "src" in this.attributes);
        }
    };
}

function setup(t, { elements = [], load = async () => {}, fetch = async () => { throw new Error("Unexpected fetch"); } } = {}) {
    const handlers = new Map();
    const pending = [];
    const parsed = [];
    const requests = [];
    const dependencies = [];
    const host = { innerHTML: "previous content" };
    // use the runtime tests' lightweight DOM-double convention; the real parser still runs
    t.mock.property(globalThis, "document", { baseURI: "https://example.test/app/" });
    t.mock.property(globalThis, "DOMParser", class {
        parseFromString(html, type) {
            assert.equal(type, "text/html");
            parsed.push(html);
            return { body: { innerHTML: html, querySelectorAll() { return elements; } } };
        }
    });
    t.mock.method(globalThis, "fetch", async url => {
        requests.push(url);
        return fetch(url);
    });
    const state = new Proxy({ value: "", src: "" }, {
        set(target, key, value) {
            const oldValue = target[key];
            target[key] = value;
            if (oldValue !== value) pending.push(Promise.resolve(handlers.get(`change:${key}`)?.({ newValue: value, oldValue })));
            return true;
        }
    });
    const controller = definition.controller({ state, host,
        events: { on(target, event, listener) { handlers.set(event, listener); } },
        loader: { async load(ids) { dependencies.push(ids); await load(ids); } }
    });
    controller.load();
    return { state, host, parsed, requests, dependencies, async flush() {
        while (pending.length) await Promise.all(pending.splice(0));
    } };
}

test("value uses the vendored parser and never fetches", async t => {
    const component = setup(t);
    component.state.value = "# Hello\n\nThis is **Markdown**.\n\n- one\n- two\n\n```js\nconst n = 1;\n```";
    await component.flush();
    assert.match(component.host.innerHTML, /<h1>Hello<\/h1>/);
    assert.match(component.host.innerHTML, /<strong>Markdown<\/strong>/);
    assert.match(component.host.innerHTML, /<ul>/);
    assert.match(component.host.innerHTML, /<code class="language-js">/);
    assert.deepEqual(component.requests, []);
});

test("src fetches a normal browser URL and renders through value", async t => {
    const component = setup(t, { fetch: async () => ({ ok: true, text: async () => "# Services" }) });
    component.state.src = source;
    await component.flush();
    assert.deepEqual(component.requests, [source]);
    assert.equal(component.state.value, "# Services");
    assert.match(component.host.innerHTML, /<h1>Services<\/h1>/);
    assert.deepEqual(component.dependencies, [[]]);
});

test("HTTP failures render status text without reading an error body", async t => {
    const component = setup(t, { fetch: async () => ({ ok: false, status: 503, statusText: "Service Unavailable" }) });
    component.state.src = source;
    await component.flush();
    assert.match(component.host.innerHTML, /503 - Service Unavailable/);
    assert.ok(component.host.innerHTML.includes(source));
});

test("relative links and media use the Markdown source and preserve URL categories", async t => {
    const cases = [
        ["a", "href", "30-modules.md", "/_assets/xshell-docs/pages/10-architecture/30-modules.md"],
        ["a", "href", "../20-components/index.md", "/_assets/xshell-docs/pages/20-components/index.md"],
        ["img", "src", "../images/services.svg", "/_assets/xshell-docs/pages/images/services.svg"],
        ["a", "href", "30-modules.md?view=all#section", "/_assets/xshell-docs/pages/10-architecture/30-modules.md?view=all#section"],
        ["a", "href", "?view=all#section", source + "?view=all#section"],
        ["source", "src", "./clip.mp4", "/_assets/xshell-docs/pages/10-architecture/clip.mp4"],
        ["video", "src", "./clip.mp4", "/_assets/xshell-docs/pages/10-architecture/clip.mp4"],
        ["audio", "src", "../clip.mp3", "/_assets/xshell-docs/pages/clip.mp3"],
        ...["#section", "/absolute/path", "https://example.com/page", "//cdn.example.com/image.svg", "mailto:user@example.com",
            "tel:+1234", "data:image/png;base64,AA", "blob:https://example.test/id"].map(url => ["a", "href", url, url])
    ];
    const elements = cases.map(([tag, attribute, value]) => element(tag, { [attribute]: value }));
    const component = setup(t, { elements, fetch: async () => ({ ok: true, text: async () => "[Modules](30-modules.md)" }) });
    component.state.src = source;
    await component.flush();
    for (const [index, [, attribute, , expected]] of cases.entries()) assert.equal(elements[index].getAttribute(attribute), expected);
});

test("cross-origin Markdown keeps its origin when resolving relative URLs", async t => {
    const anchor = element("a", { href: "next.md?q=1#part" });
    const component = setup(t, { elements: [anchor], fetch: async () => ({ ok: true, text: async () => "[Next](next.md?q=1#part)" }) });
    component.state.src = "https://docs.example.com/guides/index.md";
    await component.flush();
    assert.equal(anchor.getAttribute("href"), "https://docs.example.com/guides/next.md?q=1#part");
});

test("embedded components in nested templates load once before DOM commit", async t => {
    let release;
    const gate = new Promise(resolve => { release = resolve; });
    const elements = [element("x-button"), element("template", {}, [element("x-button"),
        element("template", {}, [element("x-icon")])])];
    const component = setup(t, { elements, load: () => gate });
    component.state.value = '<x-button>Save</x-button><template><template><x-icon></x-icon></template></template>';
    assert.deepEqual(component.dependencies, [["component:x-button", "component:x-icon"]]);
    assert.equal(component.host.innerHTML, "previous content");
    release();
    await component.flush();
    assert.match(component.host.innerHTML, /<x-button>Save<\/x-button>/);
    assert.match(component.host.innerHTML, /<template><template>/);
});

test("changing src rerenders identical Markdown for its new URL base", async t => {
    const component = setup(t, { fetch: async () => ({ ok: true, text: async () => "[Next](next.md)" }) });
    component.state.src = source;
    await component.flush();
    component.state.src = "/other/index.md";
    await component.flush();
    assert.equal(component.parsed.length, 2);
});

test("older source responses cannot overwrite newer Markdown", async t => {
    let release;
    const gate = new Promise(resolve => { release = resolve; });
    const component = setup(t, { fetch: async url => {
        if (url === source) await gate;
        return { ok: true, text: async () => url === source ? "# Old" : "# New" };
    } });
    component.state.src = source;
    component.state.src = "/new.md";
    release();
    await component.flush();
    assert.match(component.host.innerHTML, /<h1>New<\/h1>/);
});
