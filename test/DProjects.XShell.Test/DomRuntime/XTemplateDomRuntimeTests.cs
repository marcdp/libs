using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateDomRuntimeTests {

    // methods
    [Fact]
    public void DomRuntimeAppliesEventModifierFilters() {
        var cases = new[] {
            new EventModifierCase("<button x-on:click.alt=\"command\"></button>", new { altKey = true }, true),
            new EventModifierCase("<button x-on:click.alt=\"command\"></button>", new { altKey = false, altlKey = true }, false),
            new EventModifierCase("<button x-on:click.left=\"command\"></button>", new { button = 0 }, true),
            new EventModifierCase("<button x-on:click.left=\"command\"></button>", new { button = 1 }, false),
            new EventModifierCase("<button x-on:click.left=\"command\"></button>", new { button = 2 }, false),
            new EventModifierCase("<button x-on:click.middle=\"command\"></button>", new { button = 0 }, false),
            new EventModifierCase("<button x-on:click.middle=\"command\"></button>", new { button = 1 }, true),
            new EventModifierCase("<button x-on:click.middle=\"command\"></button>", new { button = 2 }, false),
            new EventModifierCase("<button x-on:click.right=\"command\"></button>", new { button = 0 }, false),
            new EventModifierCase("<button x-on:click.right=\"command\"></button>", new { button = 1 }, false),
            new EventModifierCase("<button x-on:click.right=\"command\"></button>", new { button = 2 }, true),
            new EventModifierCase("<button x-on:click.ctrl.left=\"command\"></button>", new { ctrlKey = true, button = 0 }, true),
            new EventModifierCase("<button x-on:click.ctrl.left=\"command\"></button>", new { ctrlKey = false, button = 0 }, false),
            new EventModifierCase("<button x-on:click.ctrl.left=\"command\"></button>", new { ctrlKey = true, button = 2 }, false),
            new EventModifierCase("<button x-on:click.alt.shift.right=\"command\"></button>", new { altKey = true, shiftKey = true, button = 2 }, true),
            new EventModifierCase("<button x-on:click.alt.shift.right=\"command\"></button>", new { altKey = false, shiftKey = true, button = 2 }, false)
        };

        Assert.Equal(cases.Select(@case => @case.InvokesHandler), ExecuteEventModifierFilters(cases));
    }

    [Fact]
    public void DomRuntimeAppliesAndReconcilesStructuredStyles() {
        using var result = JsonDocument.Parse(ExecuteStyleRuntime());
        var root = result.RootElement;

        Assert.Equal("none", root.GetProperty("initial").GetProperty("display").GetProperty("value").GetString());
        Assert.Equal("block", root.GetProperty("updated").GetProperty("display").GetProperty("value").GetString());
        Assert.Equal("important", root.GetProperty("updated").GetProperty("display").GetProperty("priority").GetString());
        Assert.Equal("50%", root.GetProperty("updated").GetProperty("width").GetProperty("value").GetString());
        Assert.False(root.GetProperty("updated").TryGetProperty("margin-top", out _));
        Assert.Equal("kept", root.GetProperty("updated").GetProperty("external").GetProperty("value").GetString());
        Assert.Equal(new[] { "display", "width" }, root.GetProperty("setCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(new[] { "margin-top" }, root.GetProperty("removeCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.False(root.GetProperty("attributes").TryGetProperty("style", out _));
    }

    [Fact]
    public void DomRuntimeNamedStylesUseStructuredBindingsAndRemoveNullValues() {
        using var result = JsonDocument.Parse(ExecuteNamedStyleRuntime());
        var root = result.RootElement;

        Assert.Equal("red", root.GetProperty("initial").GetProperty("border").GetProperty("value").GetString());
        Assert.Equal("8", root.GetProperty("initial").GetProperty("margin-top").GetProperty("value").GetString());
        Assert.Equal("blue", root.GetProperty("initial").GetProperty("--accent-color").GetProperty("value").GetString());
        Assert.False(root.GetProperty("updated").TryGetProperty("border", out _));
        Assert.Equal("kept", root.GetProperty("updated").GetProperty("external").GetProperty("value").GetString());
        Assert.Equal(new[] { "border", "margin-top", "--accent-color" }, root.GetProperty("initialSetCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.Empty(root.GetProperty("setCalls").EnumerateArray());
        Assert.Equal(new[] { "border" }, root.GetProperty("removeCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.False(root.GetProperty("attributes").TryGetProperty("style", out _));
        Assert.Equal(2, root.GetProperty("borderReads").GetInt32());
    }

    [Fact]
    public void DomRuntimeWholeObjectStylesUseStructuredBindingsAndRemoveNullValues() {
        using var result = JsonDocument.Parse(ExecuteWholeObjectStyleRuntime());
        var root = result.RootElement;

        Assert.Equal("red", root.GetProperty("initial").GetProperty("border").GetProperty("value").GetString());
        Assert.False(root.GetProperty("updated").TryGetProperty("border", out _));
        Assert.Equal("kept", root.GetProperty("updated").GetProperty("external").GetProperty("value").GetString());
        Assert.Equal(new[] { "border" }, root.GetProperty("initialSetCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(new[] { "border" }, root.GetProperty("removeCalls").EnumerateArray().Select(item => item.GetString()));
        Assert.False(root.GetProperty("attributes").TryGetProperty("style", out _));
    }

    [Fact]
    public void DomRuntimeReconcilesNullCollectionTransitions() {
        using var result = JsonDocument.Parse(ExecuteNullCollectionTransitions());
        var root = result.RootElement;

        Assert.Equal(new[] { 0, 2, 0, 0 }, root.GetProperty("positional").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 0, 2, 0, 0 }, root.GetProperty("keyed").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 0, 1, 2, 1, 0 }, root.GetProperty("recursive").EnumerateArray().Select(item => item.GetInt32()));
    }

    [Fact]
    public void DomRuntimePreservesConditionalVNodePositionsAcrossTransitions() {
        using var result = JsonDocument.Parse(ExecuteConditionalRuntime());
        var root = result.RootElement;
        var expected = new[] {
            new[] { "div:Ready", "#comment:x-elseif", "#comment:x-else" },
            new[] { "#comment:x-if", "div:Working", "#comment:x-else" },
            new[] { "#comment:x-if", "#comment:x-elseif", "div:Other" },
            new[] { "div:Ready", "#comment:x-elseif", "#comment:x-else" },
            new[] { "#comment:x-if", "#comment:x-elseif", "div:Other" },
            new[] { "#comment:x-if", "div:Working", "#comment:x-else" }
        };

        var readyVNodes = root.GetProperty("readyVNodes").EnumerateArray().Select(node => $"{node.GetProperty("tag").GetString()}:{node.GetProperty("label").GetString()}");
        Assert.Equal(expected[0], readyVNodes);

        var snapshots = root.GetProperty("snapshots").EnumerateArray().ToArray();
        Assert.Equal(expected.Length, snapshots.Length);
        for (var index = 0; index < expected.Length; index++) {
            var actual = snapshots[index].EnumerateArray().Select(node => $"{node.GetProperty("tag").GetString()}:{node.GetProperty("text").GetString()}");
            Assert.Equal(expected[index], actual);
        }

        Assert.Equal(new[] { 1, 2, 2, 1, 2, 2 }, root.GetProperty("statusReads").EnumerateArray().Select(item => item.GetInt32()));
    }
    // methods (private)
    private static bool[] ExecuteEventModifierFilters(IReadOnlyList<EventModifierCase> cases) {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-event-modifiers-{Guid.NewGuid():N}.mjs");
        try {
            var runs = new StringBuilder("[");
            foreach (var @case in cases) {
                if (runs.Length > 1) runs.Append(',');
                runs.Append("{event:").Append(JsonSerializer.Serialize(@case.Event));
                runs.Append(",renderer:").Append(new XTemplateCompiler().Compile(@case.Template));
                runs.Append('}');
            }
            runs.Append(']');
            File.WriteAllText(modulePath, $$"""
                class FakeFragment {
                    childNodes = [];
                    appendChild(child) { this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    cloneNode(deep) {
                        const clone = new FakeFragment();
                        if (deep) clone.childNodes = this.childNodes.map(child => child.cloneNode(true));
                        return clone;
                    }
                    querySelectorAll() { return []; }
                }
                class FakeElement {
                    constructor(tag) { this.tagName = tag.toUpperCase(); this.localName = tag.toLowerCase(); this.listeners = {}; this.childNodes = []; this.innerHTML = ""; }
                    addEventListener(name, listener) { this.listeners[name] = listener; }
                    appendChild(child) {
                        if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes);
                        else this.childNodes.push(child);
                        return child;
                    }
                    append(child) { this.appendChild(child); }
                    cloneNode(deep) {
                        const clone = new FakeElement(this.localName);
                        if (deep) clone.childNodes = this.childNodes.map(child => child.cloneNode(true));
                        return clone;
                    }
                    replaceChildren() { this.childNodes = []; }
                    setAttribute() {}
                }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) {
                        if (tag.toLowerCase() !== "template") return new FakeElement(tag);
                        const element = new FakeElement(tag);
                        element.content = new FakeFragment();
                        Object.defineProperty(element, "innerHTML", { get() { return ""; }, set() {} });
                        return element;
                    },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment() { return new FakeElement("#comment"); },
                    createTextNode() { return new FakeElement("#text"); }
                };
                const { default: createRenderEngineFactoryX } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const runs = {{runs}};
                const results = runs.map(run => {
                    const factoryContext = {};
                    const factory = createRenderEngineFactoryX.call(factoryContext, "<button></button>", {}, { render:run.renderer, dependencies:[], slots:[] });
                    factory.init();
                    const host = new FakeElement("host");
                    let calls = 0;
                    const engine = factory.create({ host, state: {}, handler: () => calls++, invalidate: () => {} });
                    engine.render();
                    host.childNodes[0].listeners.click(run.event);
                    return calls === 1;
                });
                console.log(JSON.stringify(results));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript event modifier runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<bool[]>(output) ?? throw new InvalidOperationException("JavaScript event modifier runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string ExecuteStyleRuntime() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-styles-{Guid.NewGuid():N}.mjs");
        try {
            File.WriteAllText(modulePath, $$"""
                class FakeStyle {
                    constructor() { this.values = {}; this.setCalls = []; this.removeCalls = []; }
                    setProperty(name, value, priority) { this.setCalls.push(name); this.values[name] = { value, priority }; }
                    removeProperty(name) { this.removeCalls.push(name); delete this.values[name]; }
                }
                class FakeFragment {
                    childNodes = [];
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    querySelectorAll() { return []; }
                }
                class FakeElement {
                    constructor(tag) { this.tagName = tag.toUpperCase(); this.localName = tag.toLowerCase(); this.childNodes = []; this.attributes = {}; this.style = new FakeStyle(); this.innerHTML = ""; }
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    replaceChildren() { this.childNodes = []; }
                    setAttribute(name, value) { this.attributes[name] = value; }
                    removeAttribute(name) { delete this.attributes[name]; }
                    querySelectorAll() { return []; }
                }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) { const element = new FakeElement(tag); if (tag.toLowerCase() === "template") element.content = new FakeFragment(); return element; },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment() { return new FakeElement("#comment"); },
                    createTextNode() { return new FakeElement("#text"); }
                };
                const { default: createRenderEngineFactoryX } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const renderer = (state, handler, invalidate, utils) => [utils.createVDOM("div", null, null, state.styles, null, {index:0})];
                const factoryContext = {};
                const factory = createRenderEngineFactoryX.call(factoryContext, "<div></div>", {}, { render:renderer, dependencies:[], slots:[] });
                factory.init();
                const host = new FakeElement("host");
                const state = { styles: { display:{value:"none",priority:""}, width:{value:"100%",priority:""}, "margin-top":{value:"8px",priority:""}, color:{value:"red",priority:""} } };
                const engine = factory.create({ host, state, handler:() => {}, invalidate:() => {} });
                engine.render();
                const element = host.childNodes[0];
                const initial = structuredClone(element.style.values);
                element.style.setProperty("external", "kept", "");
                element.style.setCalls = [];
                element.style.removeCalls = [];
                state.styles = { display:{value:"block",priority:"important"}, width:{value:"50%",priority:""}, color:{value:"red",priority:""} };
                engine.render();
                console.log(JSON.stringify({ initial, updated:element.style.values, setCalls:element.style.setCalls, removeCalls:element.style.removeCalls, attributes:element.attributes }));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript style runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string ExecuteNamedStyleRuntime() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-named-styles-{Guid.NewGuid():N}.mjs");
        var renderer = new XTemplateCompiler().Compile("<div x-style:border=\"state.border\" x-style:margin-top=\"state.margin\" x-style:--accent-color=\"state.accent\"></div>");
        try {
            File.WriteAllText(modulePath, $$"""
                class FakeStyle {
                    constructor() { this.values = {}; this.setCalls = []; this.removeCalls = []; }
                    setProperty(name, value, priority) { this.setCalls.push(name); this.values[name] = { value, priority }; }
                    removeProperty(name) { this.removeCalls.push(name); delete this.values[name]; }
                }
                class FakeFragment {
                    childNodes = [];
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    querySelectorAll() { return []; }
                }
                class FakeElement {
                    constructor(tag) { this.tagName = tag.toUpperCase(); this.localName = tag.toLowerCase(); this.childNodes = []; this.attributes = {}; this.style = new FakeStyle(); this.innerHTML = ""; }
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    replaceChildren() { this.childNodes = []; }
                    setAttribute(name, value) { this.attributes[name] = value; }
                    removeAttribute(name) { delete this.attributes[name]; }
                    querySelectorAll() { return []; }
                }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) { const element = new FakeElement(tag); if (tag.toLowerCase() === "template") element.content = new FakeFragment(); return element; },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment() { return new FakeElement("#comment"); },
                    createTextNode() { return new FakeElement("#text"); }
                };
                const { default: createRenderEngineFactoryX } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const renderer = {{renderer}};
                const factoryContext = {};
                const factory = createRenderEngineFactoryX.call(factoryContext, "<div></div>", {}, { render:renderer, dependencies:[], slots:[] });
                factory.init();
                let borderReads = 0;
                const state = { _border:"red", margin:8, accent:"blue" };
                Object.defineProperty(state, "border", { get() { borderReads++; return this._border; } });
                const host = new FakeElement("host");
                const engine = factory.create({ host, state, handler:() => {}, invalidate:() => {} });
                engine.render();
                const element = host.childNodes[0];
                const initial = structuredClone(element.style.values);
                const initialSetCalls = [...element.style.setCalls];
                element.style.setProperty("external", "kept", "");
                element.style.setCalls = [];
                element.style.removeCalls = [];
                state._border = null;
                engine.render();
                console.log(JSON.stringify({ initial, updated:element.style.values, initialSetCalls, setCalls:element.style.setCalls, removeCalls:element.style.removeCalls, attributes:element.attributes, borderReads }));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript named-style runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string ExecuteWholeObjectStyleRuntime() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-whole-object-style-runtime-{Guid.NewGuid():N}.mjs");
        var renderer = new XTemplateCompiler().Compile("<div x-style=\"state.styles\"></div>");
        try {
            File.WriteAllText(modulePath, $$"""
                class FakeStyle {
                    constructor() { this.values = {}; this.setCalls = []; this.removeCalls = []; }
                    setProperty(name, value, priority) { this.setCalls.push(name); this.values[name] = { value, priority }; }
                    removeProperty(name) { this.removeCalls.push(name); delete this.values[name]; }
                }
                class FakeFragment {
                    childNodes = [];
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    querySelectorAll() { return []; }
                }
                class FakeElement {
                    constructor(tag) { this.tagName = tag.toUpperCase(); this.localName = tag.toLowerCase(); this.childNodes = []; this.attributes = {}; this.style = new FakeStyle(); this.innerHTML = ""; }
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { this.appendChild(child); }
                    replaceChildren() { this.childNodes = []; }
                    setAttribute(name, value) { this.attributes[name] = value; }
                    removeAttribute(name) { delete this.attributes[name]; }
                    querySelectorAll() { return []; }
                }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) { const element = new FakeElement(tag); if (tag.toLowerCase() === "template") element.content = new FakeFragment(); return element; },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment() { return new FakeElement("#comment"); },
                    createTextNode() { return new FakeElement("#text"); }
                };
                const { default: createRenderEngineFactoryX } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const renderer = {{renderer}};
                const factory = createRenderEngineFactoryX.call({}, "<div></div>", {}, { render:renderer, dependencies:[], slots:[] });
                factory.init();
                const host = new FakeElement("host");
                const state = { styles: { border: "red" } };
                const engine = factory.create({ host, state, handler:() => {}, invalidate:() => {} });
                engine.render();
                const element = host.childNodes[0];
                const initial = structuredClone(element.style.values);
                const initialSetCalls = [...element.style.setCalls];
                element.style.setProperty("external", "kept", "");
                element.style.setCalls = [];
                element.style.removeCalls = [];
                state.styles = { border: null };
                engine.render();
                console.log(JSON.stringify({ initial, updated:element.style.values, initialSetCalls, removeCalls:element.style.removeCalls, attributes:element.attributes }));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript whole-object style reconciliation failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static string ExecuteNullCollectionTransitions() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-null-transitions-{Guid.NewGuid():N}.mjs");
        try {
            var positionalRenderer = new XTemplateCompiler().Compile("<li x-for=\"item in state.items\">{{ item.label }}</li>");
            var keyedRenderer = new XTemplateCompiler().Compile("<li x-for=\"item in state.items\" x-key=\"id\">{{ item.label }}</li>");
            var recursiveRenderer = new XTemplateCompiler().Compile("<li x-recursive=\"item in state.items\" x-key=\"id\">{{ item.label }}</li>");
            File.WriteAllText(modulePath, $$$"""
                class FakeStyle { setProperty() {} removeProperty() {} }
                class FakeNode {
                    constructor(tag, text = "") { this.localName = tag.toLowerCase(); this.tagName = tag.toUpperCase(); this.childNodes = []; this.attributes = {}; this.listeners = {}; this.style = new FakeStyle(); this._text = text; }
                    get firstChild() { return this.childNodes[0] ?? null; }
                    get lastChild() { return this.childNodes[this.childNodes.length - 1] ?? null; }
                    get textContent() { return this._text + this.childNodes.map(child => child.textContent).join(""); }
                    set textContent(value) { this._text = value ?? ""; this.childNodes = []; }
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { return this.appendChild(child); }
                    insertBefore(child, reference) { const current = this.childNodes.indexOf(child); if (current >= 0) this.childNodes.splice(current, 1); const index = reference == null ? this.childNodes.length : this.childNodes.indexOf(reference); this.childNodes.splice(index < 0 ? this.childNodes.length : index, 0, child); return child; }
                    removeChild(child) { const index = this.childNodes.indexOf(child); if (index >= 0) this.childNodes.splice(index, 1); return child; }
                    replaceChild(child, oldChild) { const index = this.childNodes.indexOf(oldChild); this.childNodes[index] = child; return oldChild; }
                    replaceChildren(...children) { this.childNodes = []; this._text = ""; for (const child of children) this.appendChild(child); }
                    setAttribute(name, value) { this.attributes[name] = value; }
                    removeAttribute(name) { delete this.attributes[name]; }
                    addEventListener(name, listener) { this.listeners[name] = listener; }
                    querySelectorAll() { return []; }
                }
                class FakeFragment extends FakeNode { constructor() { super("#fragment"); } }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) { return new FakeNode(tag); },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment(text) { return new FakeNode("#comment", text); },
                    createTextNode(text) { return new FakeNode("#text", text); }
                };
                const { default:createRenderEngineFactoryX } = await import({{{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}});
                const count = (node) => (node.localName === "li" ? 1 : 0) + node.childNodes.reduce((total, child) => total + count(child), 0);
                const create = (renderer, state) => {
                    const factory = createRenderEngineFactoryX("", {}, {render:renderer, dependencies:[], slots:[]});
                    factory.init();
                    const host = new FakeNode("host");
                    return {engine:factory.create({host, state, handler:() => {}, invalidate:() => {}}), host};
                };
                const transitionList = (renderer) => {
                    const state = {items:null};
                    const {engine, host} = create(renderer, state);
                    const result = [];
                    engine.render(); result.push(count(host));
                    state.items = [{id:1,label:"A"},{id:2,label:"B"}]; engine.render(); result.push(count(host));
                    state.items = null; engine.render(); result.push(count(host));
                    state.items = []; engine.render(); result.push(count(host));
                    return result;
                };
                const root = {id:1,label:"Root",children:null};
                const recursiveState = {items:null};
                const recursiveEngine = create({{{recursiveRenderer}}}, recursiveState);
                const recursive = [];
                recursiveEngine.engine.render(); recursive.push(count(recursiveEngine.host));
                recursiveState.items = [root]; recursiveEngine.engine.render(); recursive.push(count(recursiveEngine.host));
                root.children = [{id:2,label:"Child",children:null}]; recursiveEngine.engine.render(); recursive.push(count(recursiveEngine.host));
                root.children = null; recursiveEngine.engine.render(); recursive.push(count(recursiveEngine.host));
                recursiveState.items = null; recursiveEngine.engine.render(); recursive.push(count(recursiveEngine.host));
                console.log(JSON.stringify({positional:transitionList({{{positionalRenderer}}}), keyed:transitionList({{{keyedRenderer}}}), recursive}));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript null collection transition runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string ExecuteConditionalRuntime() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-conditional-runtime-{Guid.NewGuid():N}.mjs");
        var renderer = new XTemplateCompiler().Compile("<div x-if=\"state.status == 'ready'\">Ready</div><div x-elseif=\"state.status == 'working'\">Working</div><div x-else>Other</div>");
        try {
            File.WriteAllText(modulePath, $$$"""
                class FakeStyle { setProperty() {} removeProperty() {} }
                class FakeNode {
                    constructor(tag, text = "") { this.localName = tag.toLowerCase(); this.tagName = tag.toUpperCase(); this.childNodes = []; this.attributes = {}; this.listeners = {}; this.style = new FakeStyle(); this._text = text; }
                    get firstChild() { return this.childNodes[0] ?? null; }
                    get lastChild() { return this.childNodes[this.childNodes.length - 1] ?? null; }
                    get textContent() { return this._text + this.childNodes.map(child => child.textContent).join(""); }
                    set textContent(value) { this._text = value ?? ""; this.childNodes = []; }
                    appendChild(child) { if (child instanceof FakeFragment) this.childNodes.push(...child.childNodes); else this.childNodes.push(child); return child; }
                    append(child) { return this.appendChild(child); }
                    insertBefore(child, reference) { const index = reference == null ? this.childNodes.length : this.childNodes.indexOf(reference); this.childNodes.splice(index < 0 ? this.childNodes.length : index, 0, child); return child; }
                    removeChild(child) { const index = this.childNodes.indexOf(child); if (index >= 0) this.childNodes.splice(index, 1); return child; }
                    replaceChild(child, oldChild) { const index = this.childNodes.indexOf(oldChild); this.childNodes[index] = child; return oldChild; }
                    replaceChildren(...children) { this.childNodes = []; this._text = ""; for (const child of children) this.appendChild(child); }
                    setAttribute(name, value) { this.attributes[name] = value; }
                    removeAttribute(name) { delete this.attributes[name]; }
                    addEventListener(name, listener) { this.listeners[name] = listener; }
                    querySelectorAll() { return []; }
                }
                class FakeFragment extends FakeNode { constructor() { super("#fragment"); } }
                globalThis.DocumentFragment = FakeFragment;
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                globalThis.document = {
                    createElement(tag) { const element = new FakeNode(tag); if (tag.toLowerCase() === "template") element.content = new FakeFragment(); return element; },
                    createDocumentFragment() { return new FakeFragment(); },
                    createComment(text) { return new FakeNode("#comment", text); },
                    createTextNode(text) { return new FakeNode("#text", text); }
                };
                const { default:createRenderEngineFactoryX, XTemplateRuntimeUtils } = await import({{{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}});
                const renderer = {{{renderer}}};
                const summarizeVNode = node => ({ tag:node.tag, index:node.options.index, label:node.tag === "#comment" ? node.children : node.children[0]?.children });
                const readyVNodes = renderer({status:"ready"}, () => {}, () => {}, XTemplateRuntimeUtils, {}, 0).map(summarizeVNode);
                const factory = createRenderEngineFactoryX("", {}, {render:renderer, dependencies:[], slots:[]});
                factory.init();
                let statusReads = 0;
                const state = { _status:"ready" };
                Object.defineProperty(state, "status", { get() { statusReads++; return this._status; } });
                const host = new FakeNode("host");
                const engine = factory.create({host, state, handler:() => {}, invalidate:() => {}});
                const statuses = ["ready", "working", "other", "ready", "other", "working"];
                const snapshots = [];
                const reads = [];
                for (const status of statuses) {
                    state._status = status;
                    const before = statusReads;
                    engine.render();
                    reads.push(statusReads - before);
                    snapshots.push(host.childNodes.map(node => ({tag:node.localName, text:node.textContent})));
                }
                console.log(JSON.stringify({readyVNodes, snapshots, statusReads:reads}));
                """
            );
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript conditional runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private sealed record EventModifierCase(string Template, object Event, bool InvokesHandler);
}
