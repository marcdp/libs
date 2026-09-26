using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateCollectionConformanceTests {

    // methods
    [Fact]
    public void JavaScriptRuntimeNormalizesCollectionSourcesAccordingToTheLanguageContract() {
        Assert.Equal(new[] {
            "[]", "[]", "[1,2]", "[]", "[1]", "[1,2,3]", "[]", "[\"a\",\"😀\"]", "[]", "[\"a\",\"b\"]",
            "error", "error", "error", "error", "error", "error", "error", "error", "same-array"
        }, ExecuteCollectionNormalization());
    }
    [Fact]
    public void CompiledJavaScriptAndServerRendererMatchCollectionNormalization() {
        var values = new object?[] {
            null, Array.Empty<object>(), new object?[] { 1, 2 }, 0, 1, 3, string.Empty, "A😀",
            new Dictionary<string, object?>(), new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 }, true, false, -1, 1.5
        };
        var renderer = new XTemplateRenderer();
        var expected = values.Select(value => {
            try { return renderer.Render("<i x-for=\"item in state.items\">{{ item }}</i>", new Dictionary<string, object?> { ["items"] = value }); }
            catch (XTemplateException) { return "error"; }
        }).ToArray();

        Assert.Equal(expected, ExecuteCollectionDirectiveConformance(values));
    }
    // methods (private)
    private static string[] ExecuteCollectionNormalization() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-collection-normalization-{Guid.NewGuid():N}.mjs");
        try {
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const original = [1, 2];
                const values = [null, [], [1, 2], 0, 1, 3, "", "a😀", {}, {a:1,b:2}, true, false, -1, 1.5, NaN, Infinity, undefined, () => {}, original];
                const results = values.map((value, index) => {
                    try {
                        const normalized = XTemplateRuntimeUtils.expr.collection(value);
                        return index === values.length - 1 && normalized === original ? "same-array" : JSON.stringify(normalized);
                    } catch {
                        return "error";
                    }
                });
                console.log(JSON.stringify(results));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript collection normalization runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<string[]>(output) ?? throw new InvalidOperationException("JavaScript collection normalization runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static string[] ExecuteCollectionDirectiveConformance(IReadOnlyList<object?> values) {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-collection-directives-{Guid.NewGuid():N}.mjs");
        try {
            var renderer = new XTemplateCompiler().Compile("<i x-for=\"item in state.items\">{{ item }}</i>");
            File.WriteAllText(modulePath, $$$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}});
                const renderer = {{{renderer}}};
                const values = {{{JsonSerializer.Serialize(values)}}};
                const results = values.map(items => {
                    try {
                        const vdom = renderer({items}, null, () => {}, XTemplateRuntimeUtils, null, 0);
                        return vdom.filter(node => node.tag === "i").map(node => `<i>${node.children.map(child => child.children).join("")}</i>`).join("");
                    } catch {
                        return "error";
                    }
                });
                console.log(JSON.stringify(results));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript collection directive conformance runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<string[]>(output) ?? throw new InvalidOperationException("JavaScript collection directive conformance runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
}
