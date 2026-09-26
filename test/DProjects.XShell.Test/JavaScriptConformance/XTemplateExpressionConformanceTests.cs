using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test;

public sealed class XTemplateExpressionConformanceTests {

    // methods
    [Fact]
    public void GeneratedJavaScriptMatchesTheServerEvaluatorForExpressionConformanceCases() {
        var cases = CreateCases();
        var expected = cases.Select(EvaluateServer).ToArray();
        var actual = EvaluateJavaScript(cases);

        Assert.Equal(expected, actual);
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
    private static string Normalize(object? value) => value switch {
        null => "null",
        bool boolean => $"boolean:{boolean.ToString().ToLowerInvariant()}",
        double number => $"number:{number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}",
        string text => $"string:{text}",
        _ => "unsupported"
    };
    private sealed record ConformanceCase(string Expression, Dictionary<string, object?> State, string? Locale = null);
}
