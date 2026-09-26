using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateStyleConformanceTests {

    // methods
    [Fact]
    public void WholeObjectXPropExpandsPreservesValuesRejectsInvalidSourcesAndRespectsSourceOrder() {
        var results = ExecuteWholeObjectProperties();

        Assert.Equal(new[] { "Grid", "3", "true" }, results["basic"]);
        Assert.Equal(new[] { "true", "true", "true" }, results["identity"]);
        Assert.Equal(new[] { "spread", "Grid" }, results["namedThenSpread"]);
        Assert.Equal(new[] { "named", "Grid" }, results["spreadThenNamed"]);
        Assert.Equal(new[] { "dynamic", "spread" }, results["dynamicOrder"]);
        Assert.Equal(new[] { "null" }, results["undefined"]);
        Assert.All(results["invalid"], result => Assert.Contains("x-prop requires an object", result, StringComparison.Ordinal));
    }
    [Fact]
    public void WholeObjectXStyleExpandsStructuredStylesRejectsInvalidValuesAndRespectsSourceOrder() {
        var results = ExecuteWholeObjectStyles();

        Assert.Equal(new[] { "1px solid red", "", "8", "", "true", "", "false", "", "missing" }, results["basic"]);
        Assert.Equal(new[] { "blue", "" }, results["custom"]);
        Assert.All(results["invalid"], result => Assert.Contains("error:", result, StringComparison.Ordinal));
        Assert.Equal(new[] { "blue", "red", "none !important", "", "blue", "red" }, results["order"]);
        Assert.All(results["invalidMembers"], result => Assert.Contains("error:", result, StringComparison.Ordinal));
    }
    [Fact]
    public void GeneratedJavaScriptNamedStylesPreserveSourceOrderNullScalarAndPrioritySemantics() {
        using var result = JsonDocument.Parse(ExecuteNamedStyleCompilerSemantics());
        var root = result.RootElement;

        Assert.Equal("blue", root.GetProperty("literalThenDynamic").GetProperty("border").GetProperty("value").GetString());
        Assert.Equal("red", root.GetProperty("dynamicThenLiteral").GetProperty("border").GetProperty("value").GetString());
        Assert.Equal("red", root.GetProperty("literalThenNull").GetProperty("border").GetProperty("value").GetString());
        Assert.Equal("red", root.GetProperty("dynamicThenNull").GetProperty("border").GetProperty("value").GetString());
        Assert.Equal("none !important", root.GetProperty("dynamicImportant").GetProperty("display").GetProperty("value").GetString());
        Assert.Equal(string.Empty, root.GetProperty("dynamicImportant").GetProperty("display").GetProperty("priority").GetString());
        Assert.Equal("error", root.GetProperty("objectValue").GetString());
        Assert.Equal("error", root.GetProperty("arrayValue").GetString());
    }
    [Fact]
    public void JavaScriptRuntimeRejectsSpreadAndDynamicGenericStyleAttributes() {
        Assert.Equal(new[] { true, true }, ExecuteGenericStyleRejections());
    }
    // methods (private)
    private static string ExecuteNamedStyleCompilerSemantics() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-named-style-semantics-{Guid.NewGuid():N}.mjs");
        var compiler = new XTemplateCompiler();
        var literalThenDynamic = compiler.Compile("<div style=\"border:red\" x-style:border=\"state.border\"></div>");
        var dynamicThenLiteral = compiler.Compile("<div x-style:border=\"state.border\" style=\"border:red\"></div>");
        var literalThenNull = compiler.Compile("<div style=\"border:red\" x-style:border=\"state.border\"></div>");
        var dynamicThenNull = compiler.Compile("<div x-style:border=\"state.border\" style=\"border:red\"></div>");
        var dynamicImportant = compiler.Compile("<div x-style:display=\"state.display\"></div>");
        var objectValue = compiler.Compile("<div x-style:border=\"state.border\"></div>");
        var arrayValue = compiler.Compile("<div x-style:border=\"state.border\"></div>");
        try {
            File.WriteAllText(modulePath, $$$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}});
                const runs = {
                    literalThenDynamic: { renderer:{{{literalThenDynamic}}}, state:{border:"blue"} },
                    dynamicThenLiteral: { renderer:{{{dynamicThenLiteral}}}, state:{border:"blue"} },
                    literalThenNull: { renderer:{{{literalThenNull}}}, state:{border:null} },
                    dynamicThenNull: { renderer:{{{dynamicThenNull}}}, state:{border:null} },
                    dynamicImportant: { renderer:{{{dynamicImportant}}}, state:{display:"none !important"} },
                    objectValue: { renderer:{{{objectValue}}}, state:{border:{}} },
                    arrayValue: { renderer:{{{arrayValue}}}, state:{border:[]} }
                };
                const result = Object.fromEntries(Object.entries(runs).map(([name, run]) => {
                    try { return [name, run.renderer(run.state, null, () => {}, XTemplateRuntimeUtils, null, 0)[0].styles]; }
                    catch { return [name, "error"]; }
                }));
                console.log(JSON.stringify(result));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript named-style semantic runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static bool[] ExecuteGenericStyleRejections() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-style-rejections-{Guid.NewGuid():N}.mjs");
        try {
            var spread = new XTemplateCompiler().Compile("<div x-attr=\"state.attributes\"></div>");
            var dynamic = new XTemplateCompiler().Compile("<div x-attr:[state.name]=\"state.value\"></div>");
            File.WriteAllText(modulePath, $$$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}});
                const cases = [
                    { renderer:{{{spread}}}, state:{attributes:{STYLE:"display:none"}} },
                    { renderer:{{{dynamic}}}, state:{name:"Style",value:"display:none"} }
                ];
                console.log(JSON.stringify(cases.map(run => { try { run.renderer(run.state, null, () => {}, XTemplateRuntimeUtils, null, 0); return false; } catch { return true; } })));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript generic style rejection runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<bool[]>(output) ?? throw new InvalidOperationException("JavaScript generic style rejection runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static Dictionary<string, string[]> ExecuteWholeObjectProperties() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-whole-object-properties-{Guid.NewGuid():N}.mjs");
        try {
            var compiler = new XTemplateCompiler();
            var basic = compiler.Compile("<x-grid x-prop=\"state.props\"></x-grid>");
            var identity = compiler.Compile("<x-grid x-prop=\"state.props\"></x-grid>");
            var namedThenSpread = compiler.Compile("<x-grid x-prop:items=\"state.items\" x-prop=\"state.props\"></x-grid>");
            var spreadThenNamed = compiler.Compile("<x-grid x-prop=\"state.props\" x-prop:items=\"state.specialItems\"></x-grid>");
            var dynamicThenSpread = compiler.Compile("<x-grid x-prop:[state.name]=\"state.value\" x-prop=\"state.props\"></x-grid>");
            var spreadThenDynamic = compiler.Compile("<x-grid x-prop=\"state.props\" x-prop:[state.name]=\"state.value\"></x-grid>");
            var undefined = compiler.Compile("<x-grid x-prop=\"state.props\"></x-grid>");
            var invalid = new[] { "null", "[]", "'text'", "1", "true" }.Select(value => compiler.Compile($"<div x-prop=\"state.value\"></div>")).ToArray();
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const run = (renderer, state) => renderer(state, null, () => {}, XTemplateRuntimeUtils, null, 0)[0].props;
                const items = [{ id: 1 }];
                const config = { mode: "compact" };
                const basic = run({{basic}}, { props: { title: "Grid", count: 3, enabled: true } });
                const identityState = { props: { items, config, nothing: null } };
                const identity = run({{identity}}, identityState);
                const namedThenSpread = run({{namedThenSpread}}, { items: "named", props: { items: "spread", title: "Grid" } });
                const spreadThenNamed = run({{spreadThenNamed}}, { props: { items: "spread", title: "Grid" }, specialItems: "named" });
                const dynamicOrder = [
                    run({{spreadThenDynamic}}, { props: { items: "spread" }, name: "items", value: "dynamic" }).items,
                    run({{dynamicThenSpread}}, { props: { items: "spread" }, name: "items", value: "dynamic" }).items
                ];
                const undefinedValue = run({{undefined}}, { props: { value: undefined } }).value;
                const invalid = [{{string.Join(",", invalid.Select((renderer, index) => $"{{renderer:{renderer},state:{{value:{new[] { "null", "[]", "'text'", "1", "true" }[index]}}}}}"))}}].map(({renderer, state}) => {
                    try { renderer(state, null, () => {}, XTemplateRuntimeUtils, null, 0); return "unexpected"; }
                    catch (error) { return "error:" + error.message; }
                });
                console.log(JSON.stringify({
                    basic: [basic.title, String(basic.count), String(basic.enabled)],
                    identity: [identity.items === items, identity.config === config, identity.nothing === null].map(String),
                    namedThenSpread: [namedThenSpread.items, namedThenSpread.title],
                    spreadThenNamed: [spreadThenNamed.items, spreadThenNamed.title],
                    dynamicOrder: [dynamicOrder[0], dynamicOrder[1]],
                    undefined: [String(undefinedValue)],
                    invalid
                }));
                """.Replace("undefined: [String(undefinedValue)]", "undefined: [undefinedValue === null ? \"null\" : String(undefinedValue)]"));
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript whole-object x-prop runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(output) ?? throw new InvalidOperationException("JavaScript whole-object x-prop runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static Dictionary<string, string[]> ExecuteWholeObjectStyles() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-whole-object-styles-{Guid.NewGuid():N}.mjs");
        try {
            var compiler = new XTemplateCompiler();
            var basic = compiler.Compile("<div x-style=\"state.styles\"></div>");
            var literalThenSpread = compiler.Compile("<div style=\"border:red\" x-style=\"state.styles\"></div>");
            var spreadThenLiteral = compiler.Compile("<div x-style=\"state.styles\" style=\"border:red\"></div>");
            var spreadThenNamed = compiler.Compile("<div x-style=\"state.styles\" x-style:border=\"state.border\"></div>");
            var namedThenSpread = compiler.Compile("<div x-style:border=\"state.border\" x-style=\"state.styles\"></div>");
            var important = compiler.Compile("<div x-style=\"state.styles\"></div>");
            var invalid = new[] { "null", "[]", "'text'", "1", "true" }.Select(_ => compiler.Compile("<div x-style=\"state.value\"></div>")).ToArray();
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const summarize = styles => Object.fromEntries(Object.entries(styles).map(([name, value]) => [name, [value.value, value.priority]]));
                const run = (renderer, state) => summarize(renderer(state, null, () => {}, XTemplateRuntimeUtils, null, 0)[0].styles);
                const invalid = [{{string.Join(",", invalid.Select((renderer, index) => $"{{renderer:{renderer},state:{{value:{new[] { "null", "[]", "'text'", "1", "true" }[index]}}}}}"))}}].map(({renderer, state}) => {
                    try { renderer(state, null, () => {}, XTemplateRuntimeUtils, null, 0); return "unexpected"; }
                    catch (error) { return "error:" + error.message; }
                });
                const invalidMembers = [
                    { styles: { border: {} } },
                    { styles: { border: [] } },
                    { styles: { "margin.top": "red" } }
                ].map(state => {
                    try { run({{basic}}, state); return "unexpected"; }
                    catch (error) { return "error:" + error.message; }
                });
                const basic = run({{basic}}, { styles: { border: "1px solid red", "margin-top": 8, "--accent-color": true, display: null, visibility: false } });
                const custom = run({{basic}}, { styles: { "--Accent-Color": "blue" } });
                const importantStyle = run({{important}}, { styles: { display: "none !important" } }).display;
                const spreadThenNamedStyle = run({{spreadThenNamed}}, { styles: { border: "red" }, border: "blue" }).border;
                const namedThenSpreadStyle = run({{namedThenSpread}}, { styles: { border: "red" }, border: "blue" }).border;
                console.log(JSON.stringify({
                    basic: [basic.border[0], basic.border[1], basic["margin-top"][0], basic["margin-top"][1], basic["--accent-color"][0], basic["--accent-color"][1], basic.visibility[0], basic.visibility[1], basic.display === undefined ? "missing" : "present"],
                    custom: custom["--Accent-Color"],
                    invalid,
                    invalidMembers,
                    order: [run({{literalThenSpread}}, { styles: { border: "blue" } }).border[0], run({{spreadThenLiteral}}, { styles: { border: "blue" } }).border[0], importantStyle[0], importantStyle[1], spreadThenNamedStyle[0], namedThenSpreadStyle[0]]
                }));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript whole-object x-style runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(output) ?? throw new InvalidOperationException("JavaScript whole-object x-style runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
}
