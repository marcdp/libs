using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateJavaScriptConformanceTests {

    // methods
    [Fact]
    public void GeneratedJavaScriptMatchesTheServerEvaluatorForExpressionConformanceCases() {
        var cases = CreateCases();
        var expected = cases.Select(EvaluateServer).ToArray();
        var actual = EvaluateJavaScript(cases);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BrowserRadioModelUsesScalarComparisonResolvedValuesAndRejectsNonScalars() {
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

    [Fact]
    public void GeneratedJavaScriptPreservesTransformerNullLaziness() {
        var state = new Dictionary<string, object?>();
        var results = EvaluateJavaScript([
            new ConformanceCase("null | number(1 / 0)", state),
            new ConformanceCase("null | number(1 / 0) | upper", state),
            new ConformanceCase("1 | number(1 / 0)", state)
        ]);

        Assert.Equal(new[] { "null", "null", "error" }, results);
    }

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
    public void GeneratedJavaScriptMatchesTheInvariantTransformerProfileWithoutALocale() {
        var state = new Dictionary<string, object?>();
        var cases = new[] {
            new ConformanceCase("1234.5 | number(2)", state),
            new ConformanceCase("0.25 | percent(1)", state),
            new ConformanceCase("1234.5 | currency('EUR')", state),
            new ConformanceCase("'2026-09-24' | date('MMMM')", state),
            new ConformanceCase("'i' | upper", state),
            new ConformanceCase("'I' | lower", state)
        };

        Assert.Equal(cases.Select(EvaluateServer), EvaluateJavaScript(cases));
    }

    [Fact]
    public void GeneratedJavaScriptMatchesInvariantLargeNumberFormatting() {
        var state = new Dictionary<string, object?>();
        var cases = new[] {
            new ConformanceCase("100000000000000000000 | number(0)", state),
            new ConformanceCase("1000000000000000000000 | number(0)", state),
            new ConformanceCase("10000000000000000000000 | number(0)", state),
            new ConformanceCase("-1000000000000000000000 | number(0)", state),
            new ConformanceCase("1000000000000000000000 | number(2)", state),
            new ConformanceCase("100000000000000000000 | percent(0)", state),
            new ConformanceCase("1000000000000000000000 | currency('EUR')", state)
        };

        Assert.Equal(cases.Select(EvaluateServer), EvaluateJavaScript(cases));
    }

    [Fact]
    public void GeneratedJavaScriptMatchesUnicodeStringPredicateSemantics() {
        var state = new Dictionary<string, object?>();
        var cases = new[] {
            new ConformanceCase("'A😀B' | contains('😀')", state),
            new ConformanceCase("'A😀' | endsWith('😀')", state),
            new ConformanceCase("'😀ABC' | startsWith('😀')", state),
            new ConformanceCase("'A😀B' | contains('😃')", state),
            new ConformanceCase("'abc😀def' | contains('😀d')", state)
        };

        Assert.Equal(cases.Select(EvaluateServer), EvaluateJavaScript(cases));
    }

    [Fact]
    public void BrowserRuntimeAppliesEventModifierFilters() {
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
    public void BrowserRuntimeAppliesAndReconcilesStructuredStylesThroughCssom() {
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
    public void BrowserNamedStylesUseStructuredCssomBindingsAndRemoveNullValues() {
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
    public void BrowserNamedStylesPreserveSourceOrderNullScalarAndPrioritySemantics() {
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
    public void BrowserRuntimeRejectsSpreadAndDynamicGenericStyleAttributes() {
        Assert.Equal(new[] { true, true }, ExecuteGenericStyleRejections());
    }

    [Fact]
    public void BrowserRuntimeNormalizesCollectionSourcesAccordingToTheLanguageContract() {
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

    [Fact]
    public void BrowserRuntimeReconcilesNullCollectionTransitions() {
        using var result = JsonDocument.Parse(ExecuteNullCollectionTransitions());
        var root = result.RootElement;

        Assert.Equal(new[] { 0, 2, 0, 0 }, root.GetProperty("positional").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 0, 2, 0, 0 }, root.GetProperty("keyed").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { 0, 1, 2, 1, 0 }, root.GetProperty("recursive").EnumerateArray().Select(item => item.GetInt32()));
    }

    // methods (private)
    private static IReadOnlyList<ConformanceCase> CreateCases() {
        var state = new Dictionary<string, object?> {
            ["name"] = "  ada  ",
            ["missing"] = null,
            ["text"] = "A😀",
            ["items"] = new object?[] { new Dictionary<string, object?> { ["name"] = "A" }, new Dictionary<string, object?> { ["name"] = "B" } },
            ["object"] = new Dictionary<string, object?> { ["name"] = "Ada" },
            ["number"] = 12.3456,
            ["ratio"] = 0.25,
            ["maximum"] = double.MaxValue,
            ["code"] = " eur "
        };
        return [
            new("null", state), new("true", state), new("12.5", state), new("'text'", state),
            new("state.name", state), new("state.object.name", state), new("state.object.missing", state), new("state.missing.name", state),
            new("state.text.length", state), new("state.items.length", state), new("state.object['name']", state), new("state.items[1].name", state),
            new("!false", state), new("+12", state), new("-12", state), new("1 + 2", state), new("5 - 2", state), new("2 * 3", state), new("8 / 2", state), new("8 % 3", state),
            new("1 == 1", state), new("1 != '1'", state), new("'a' < '😀'", state), new("2 <= 2", state), new("3 > 2", state), new("3 >= 3", state),
            new("false && (1 / 0)", state), new("true || (1 / 0)", state), new("1 ?? (1 / 0)", state), new("true ? 1 : (1 / 0)", state), new("false ? (1 / 0) : 2", state),
            new("null | number(1 / 0)", state), new("state.name | trim | upper", state), new("state.number | number(2)", state, "en-US"), new("state.ratio | percent(1)", state, "en-US"),
            new("'abcdef' | startsWith('abc')", state), new("'abcdef' | startsWith('def')", state), new("'abcdef' | endsWith('def')", state), new("'abcdef' | endsWith('abc')", state),
            new("'abcdef' | contains('cd')", state), new("'abcdef' | contains('xy')", state), new("state.name | trim | startsWith('a')", state), new("12.5 | number(1) | endsWith('5')", state),
            new("null | endsWith(1 / 0)", state), new("1 | startsWith('1')", state), new("'abc' | startsWith(1)", state), new("'abc' | startsWith()", state), new("'abc' | startsWith('a', 'b')", state),
            new("1 | endsWith('1')", state), new("'abc' | endsWith(1)", state), new("'abc' | contains()", state), new("'abc' | contains('a', 'b')", state), new("1 | contains('1')", state), new("'abc' | contains(1)", state), new("'abc' | endsWith('c') | upper", state),
            new("1234.5 | currency('EUR')", state, "en-US"), new("1234.5 | currency('EUR')", state, "es-ES"), new("1234.5 | currency('EUR')", state, "tr-TR"),
            new("1234.5 | currency('JPY')", state, "en-US"), new("1234.5 | currency('CAD')", state, "en-US"), new("1234.5 | currency('AUD')", state, "en-US"), new("1234.5 | currency('CNY')", state, "en-US"),
            new("'2026-09-24' | date('MMMM')", state, "en-US"), new("'2026-09-24' | date('MMMM')", state, "es-ES"), new("'2026-09-24' | date('MMMM')", state, "tr-TR"),
            new("'2026-09-24T21:15:00+02:00' | datetime('yyyy-MM-dd HH:mm:ss')", state), new("'2026-09-24T21:15:00Z' | time('HH:mm')", state),
            new("1.25 | number(1)", state), new("-1.25 | number(1)", state), new("1.005 | number(2)", state), new("-1.005 | number(2)", state), new("2.675 | number(2)", state), new("-2.675 | number(2)", state), new("12.345 | number(2)", state), new("12.3455 | number(3)", state), new("12.3456 | number(3)", state), new("0.0005 | number(3)", state), new("999999999999999 | number(0)", state), new("state.maximum | percent", state),
            new("'2024-02-29' | date('yyyy-MM-dd')", state), new("'2025-02-29' | date('yyyy-MM-dd')", state), new("'2026-04-31' | date('yyyy-MM-dd')", state), new("'2026-12-31' | date('yyyy-MM-dd')", state), new("'2026-02-30' | date('yyyy-MM-dd')", state), new("'2026-13-01' | date('yyyy-MM-dd')", state), new("'2026-00-10' | date('yyyy-MM-dd')", state), new("'2026-09-24T25:00:00Z' | time('HH:mm')", state), new("'2026-09-24T21:60:00Z' | time('HH:mm')", state), new("'2026-09-24T21:15:60Z' | time('HH:mm')", state), new("'2026-09-24T21:15:00+14:01' | time('HH:mm')", state), new("'2026-09-24T21:15:00' | time('HH:mm')", state),
            new("'2026-09-24' | date('QQ')", state), new("'2026-09-24T21:15:00Z' | datetime('yyyy')", state), new("'2026-09-24T21:15:00Z' | datetime('HH:mm')", state), new("'2026-09-24T21:15:00Z' | date('HH:mm')", state)
        ];
    }

    private static string EvaluateServer(ConformanceCase test) {
        try {
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = test.State }, locale: test.Locale);
            return Normalize(XTemplateExpressions.Evaluate(test.Expression, context));
        } catch (XTemplateExpressionException) {
            return "error";
        }
    }

    private static string[] EvaluateJavaScript(IReadOnlyList<ConformanceCase> cases) {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        Assert.True(File.Exists(runtimePath), $"The copied browser runtime was not found at '{runtimePath}'.");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-conformance-{Guid.NewGuid():N}.mjs");
        try {
            File.WriteAllText(modulePath, CreateJavaScriptModule(runtimePath, cases));
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"JavaScript conformance runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<string[]>(output) ?? throw new InvalidOperationException("JavaScript conformance runtime returned no results.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

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

    private static string CreateJavaScriptModule(string runtimePath, IReadOnlyList<ConformanceCase> cases) {
        var runs = new StringBuilder("[");
        foreach (var test in cases) {
            if (runs.Length > 1) runs.Append(',');
            runs.Append("{state:").Append(JsonSerializer.Serialize(test.State));
            if (test.Locale != null) runs.Append(",locale:").Append(JsonSerializer.Serialize(test.Locale));
            runs.Append(",renderer:").Append(new XTemplateCompiler().Compile($"<input x-prop:value=\"{test.Expression}\">"));
            runs.Append('}');
        }
        runs.Append(']');
        return $$"""
            globalThis.HTMLElement = class {};
            globalThis.CSSStyleSheet = class { replaceSync() {} };
            globalThis.customElements = { get() {}, define() {} };
            globalThis.window = { customElements: globalThis.customElements };
            const { XTemplateRuntimeUtils } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
            const runs = {{runs}};
            const normalize = value => value === null ? "null" : typeof value === "boolean" ? `boolean:${value}` : typeof value === "number" ? `number:${value}` : typeof value === "string" ? `string:${value}` : "unsupported";
            const results = runs.map(run => {
                try {
                    const i18n = run.locale === undefined ? null : { config: { lang: run.locale } };
                    const result = run.renderer(run.state, null, () => {}, XTemplateRuntimeUtils, i18n, 0);
                    return normalize(result[0].props.value);
                } catch (error) {
                    return "error";
                }
            });
            console.log(JSON.stringify(results));
            """;
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

    private static string Normalize(object? value) => value switch {
        null => "null",
        bool boolean => $"boolean:{boolean.ToString().ToLowerInvariant()}",
        double number => $"number:{number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}",
        string text => $"string:{text}",
        _ => "unsupported"
    };

    private sealed record ConformanceCase(string Expression, Dictionary<string, object?> State, string? Locale = null);
    private sealed record RadioCase(object? Model, object? RadioValue, bool Dynamic = false);
    private sealed record AssignmentCase(string Template, Dictionary<string, object?> State, string Value, bool Fails, bool IsLoop = false);
    private sealed record EventModifierCase(string Template, object Event, bool InvokesHandler);
}
