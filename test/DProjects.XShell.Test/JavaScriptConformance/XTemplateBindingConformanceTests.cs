using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateBindingConformanceTests {

    // methods
    [Fact]
    public void JavaScriptRuntimeRadioModelUsesScalarComparisonResolvedValuesAndRejectsNonScalars() {
        var cases = new[] {
            new RadioCase("a", "a"),
            new RadioCase("b", "a"),
            new RadioCase(1, "1"),
            new RadioCase(2, "1"),
            new RadioCase(true, "true"),
            new RadioCase(false, "false"),
            new RadioCase(null, ""),
            new RadioCase(1, 1, true),
            new RadioCase(1, "1", true),
            new RadioCase(new Dictionary<string, object?>(), "a"),
            new RadioCase(Array.Empty<object?>(), "a")
        };

        Assert.Equal(new[] { "true", "false", "true", "false", "true", "true", "false", "true", "true", "error", "error" }, ExecuteRadioChecked(cases));
    }
    [Fact]
    public void GeneratedJavaScriptUsesStrictSingleEvaluationAssignmentPaths() {
        var cases = new[] {
            new AssignmentCase("<input x-model=\"state.name\">", new Dictionary<string, object?> { ["name"] = "old" }, "updated", false),
            new AssignmentCase("<input x-model=\"state.user.name\">", new Dictionary<string, object?> { ["user"] = new Dictionary<string, object?> { ["name"] = "old" } }, "updated", false),
            new AssignmentCase("<input x-model=\"state.object['name']\">", new Dictionary<string, object?> { ["object"] = new Dictionary<string, object?> { ["name"] = "old" } }, "updated", false),
            new AssignmentCase("<input x-model=\"state.items[0]\">", new Dictionary<string, object?> { ["items"] = new[] { "old" } }, "updated", false),
            new AssignmentCase("<input x-for=\"item in state.items\" x-model=\"state.items[index].name\">", new Dictionary<string, object?> { ["items"] = new object?[] { new Dictionary<string, object?> { ["name"] = "first" }, new Dictionary<string, object?> { ["name"] = "second" } } }, "updated", false, true),
            new AssignmentCase("<input x-model=\"state.groups[state.groupIndex].items[state.itemIndex].name\">", new Dictionary<string, object?> { ["groupIndex"] = 0, ["itemIndex"] = 0, ["groups"] = new object?[] { new Dictionary<string, object?> { ["items"] = new object?[] { new Dictionary<string, object?> { ["name"] = "old" } } } } }, "updated", false),
            new AssignmentCase("<input x-model=\"state.items[-1]\">", new Dictionary<string, object?> { ["items"] = new[] { "old" } }, "updated", true),
            new AssignmentCase("<input x-model=\"state.items[999]\">", new Dictionary<string, object?> { ["items"] = new[] { "old" } }, "updated", true),
            new AssignmentCase("<input x-model=\"state.object[1]\">", new Dictionary<string, object?> { ["object"] = new Dictionary<string, object?> { ["name"] = "old" } }, "updated", true),
            new AssignmentCase("<input x-model=\"state.items['0']\">", new Dictionary<string, object?> { ["items"] = new[] { "old" } }, "updated", true),
            new AssignmentCase("<input x-model=\"state.user.name\">", new Dictionary<string, object?> { ["user"] = null }, "updated", true),
            new AssignmentCase("<input x-model=\"state.missing.name\">", new Dictionary<string, object?>(), "updated", true),
            new AssignmentCase("<input x-model=\"state.created\">", new Dictionary<string, object?>(), "updated", true)
        };

        var results = ExecuteAssignments(cases);

        Assert.Equal("{\"name\":\"updated\"}", results[0]);
        Assert.Equal("{\"user\":{\"name\":\"updated\"}}", results[1]);
        Assert.Equal("{\"object\":{\"name\":\"updated\"}}", results[2]);
        Assert.Equal("{\"items\":[\"updated\"]}", results[3]);
        Assert.Equal("{\"items\":[{\"name\":\"first\"},{\"name\":\"updated\"}]}", results[4]);
        Assert.Equal("{\"groupIndex\":0,\"itemIndex\":0,\"groups\":[{\"items\":[{\"name\":\"updated\"}]}]}", results[5]);
        Assert.Equal(Enumerable.Repeat("error", 7), results.Skip(6));
    }
    [Fact]
    public void GeneratedJavaScriptEvaluatesNestedAssignmentIndexesOnceFromLeftToRight() {
        Assert.Equal("{\"order\":[\"group\",\"item\"],\"name\":\"updated\"}", ExecuteInstrumentedNestedAssignment());
    }
    // methods (private)
    private static string[] ExecuteRadioChecked(IReadOnlyList<RadioCase> cases) {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-radio-{Guid.NewGuid():N}.mjs");
        try {
            var runs = new StringBuilder("[");
            foreach (var test in cases) {
                if (runs.Length > 1) runs.Append(',');
                var state = new Dictionary<string, object?> { ["choice"] = test.Model, ["radioValue"] = test.Dynamic ? test.RadioValue : null };
                runs.Append("{state:").Append(JsonSerializer.Serialize(state)).Append(",renderer:");
                var template = test.Dynamic ? "<input type=\"radio\" x-attr:value=\"state.radioValue\" x-model=\"state.choice\">" : $"<input type=\"radio\" value=\"{test.RadioValue}\" x-model=\"state.choice\">";
                runs.Append(new XTemplateCompiler().Compile(template)).Append('}');
            }
            runs.Append(']');
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                XTemplateRuntimeUtils.rewriteAttribute = (tag, attrs, attr, value) => value;
                const runs = {{runs}};
                const results = runs.map(run => {
                    try {
                        const input = run.renderer(run.state, null, () => {}, XTemplateRuntimeUtils, null, 0)[0];
                        const checked = typeof input.props.checked === "function" ? input.props.checked.call(input) : input.props.checked;
                        return String(checked);
                    }
                    catch (error) { return "error"; }
                });
                console.log(JSON.stringify(results));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript radio runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<string[]>(output) ?? throw new InvalidOperationException("JavaScript radio runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static string[] ExecuteAssignments(IReadOnlyList<AssignmentCase> cases) {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-assignments-{Guid.NewGuid():N}.mjs");
        try {
            var runs = new StringBuilder("[");
            foreach (var test in cases) {
                if (runs.Length > 1) runs.Append(',');
                runs.Append("{state:").Append(JsonSerializer.Serialize(test.State));
                runs.Append(",loop:").Append(test.IsLoop ? "true" : "false");
                runs.Append(",value:").Append(JsonSerializer.Serialize(test.Value));
                runs.Append(",renderer:").Append(new XTemplateCompiler().Compile(test.Template));
                runs.Append('}');
            }
            runs.Append(']');
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const runs = {{runs}};
                const results = runs.map(run => {
                    try {
                        const result = run.renderer(run.state, null, () => {}, XTemplateRuntimeUtils, { config: { lang: "en-US" } }, 0);
                        const input = run.loop ? result[2] : result[0];
                        input.events["change.stop"]({ target: { localName: "input", value: run.value } });
                        return JSON.stringify(run.state);
                    } catch (error) {
                        return "error";
                    }
                });
                console.log(JSON.stringify(results));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript assignment runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<string[]>(output) ?? throw new InvalidOperationException("JavaScript assignment runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private static string ExecuteInstrumentedNestedAssignment() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-assignment-order-{Guid.NewGuid():N}.mjs");
        try {
            var renderer = new XTemplateCompiler().Compile("<input x-model=\"state.groups[state.groupIndex].items[state.itemIndex].name\">");
            File.WriteAllText(modulePath, $$"""
                globalThis.HTMLElement = class {};
                globalThis.CSSStyleSheet = class { replaceSync() {} };
                globalThis.customElements = { get() {}, define() {} };
                globalThis.window = { customElements: globalThis.customElements };
                const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
                const order = [];
                const state = { groups: [{ items: [{ name: "old" }] }] };
                Object.defineProperties(state, {
                    groupIndex: { enumerable:true, get() { order.push("group"); return 0; } },
                    itemIndex: { enumerable:true, get() { order.push("item"); return 0; } }
                });
                const renderer = {{renderer}};
                const input = renderer(state, null, () => {}, XTemplateRuntimeUtils, { config: { lang: "en-US" } }, 0)[0];
                order.length = 0;
                input.events["change.stop"]({ target: { localName: "input", value: "updated" } });
                console.log(JSON.stringify({ order, name: state.groups[0].items[0].name }));
                """);
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript assignment order runtime failed:{Environment.NewLine}{error}");
            return output.Trim();
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }
    private sealed record RadioCase(object? Model, object? RadioValue, bool Dynamic = false);
    private sealed record AssignmentCase(string Template, Dictionary<string, object?> State, string Value, bool Fails, bool IsLoop = false);
}
