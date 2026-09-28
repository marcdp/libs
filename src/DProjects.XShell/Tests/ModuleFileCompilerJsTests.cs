using System.Text.Json;
using System.Text.RegularExpressions;

using DProjects.XShell.Services;
using Xunit;

namespace DProjects.XShell.Tests {

    public class ModuleFileCompilerJsTests {

        // methods
        [Theory]
        [InlineData("\".x { background: url('./image.png'); }\"")]
        [InlineData("'.x { background: url(\"./image.png\"); }'")]
        [InlineData("`.x { background: url('./image.png'); }`")]
        public void CompileDecodesProcessesAndSerializesStaticCss(string style) {
            // compile each supported static JavaScript string form
            var result = Compile($"export default {{ style: {style} }};");

            // verify the CSS compiler saw the decoded value and the result remains a JavaScript string
            Assert.Contains("/components/image.png", ReadProperty(result.Content, "style"));
            Assert.StartsWith("export default", result.Content);
        }

        [Theory]
        [InlineData("\"<img src='./image.png'>\"")]
        [InlineData("'<img src=\"./image.png\">'")]
        [InlineData("`<img src='./image.png'>`")]
        public void CompileDecodesProcessesAndSerializesStaticHtml(string template) {
            // compile each supported static JavaScript string form
            var result = Compile($"export default {{ template: {template} }};");

            // verify the HTML compiler saw the decoded value and the result remains a JavaScript string
            Assert.Contains("/components/image.png", ReadProperty(result.Content, "template"));
        }

        [Fact]
        public void CompileSerializesQuotesBackslashesNewlinesBackticksAndSubstitutionText() {
            // compile content containing characters that require safe JavaScript serialization
            const string source = """export default { style: ".x::before { content: \"C:\\temp ${value} `\"; }\n.y { background: url('./image.png'); }" };""";
            var result = Compile(source);

            // deserialize the generated literal to verify its exact runtime value
            var style = ReadProperty(result.Content, "style");
            Assert.Contains("content: \"C:\\temp ${value} `\";", style);
            Assert.Contains('\n', style);
            Assert.Contains("/components/image.png", style);
        }

        [Fact]
        public void CompileSerializesProcessedHtmlContainingQuotes() {
            // compile HTML whose quote delimiters differ from the containing JavaScript string
            var result = Compile("export default { template: `<img title=\"quoted\" src='./image.png'>` };");

            // verify both HTML quotes and the rewritten URL survive serialization
            Assert.Equal("<img title=\"quoted\" src='/components/image.png'>", ReadProperty(result.Content, "template"));
        }

        [Theory]
        [InlineData("style", "getStyle()", "'style' must be a static string.")]
        [InlineData("template", "getTemplate()", "'template' must be a static string.")]
        [InlineData("style", "`color:${value}`", "'style' must be a static string.")]
        [InlineData("template", "`<p>${value}</p>`", "'template' must be a static string.")]
        public void CompileRejectsNonStaticValues(string property, string value, string message) {
            // reject expressions and substitution-bearing templates before content compilation
            var exception = Assert.Throws<InvalidOperationException>(() => Compile($"export default {{ {property}: {value} }};"));

            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void CompileRunsXTemplateCompilationAfterTemplateSerialization() {
            // compile an X Template that starts as a static JavaScript string
            var result = Compile("export default { template: `<p>Hello</p>` };", "x");

            // verify templateRenderer is generated after the template becomes a JSON string literal
            Assert.Contains("templateRenderer:", result.Content);
            Assert.Contains("render:", result.Content);
            Assert.Equal("<p>Hello</p>", ReadProperty(result.Content, "template"));
        }

        [Fact]
        public void CompileReplacesExistingTemplateRenderer() {
            // compile a definition that already contains the generated artifact property
            var result = Compile("export default { template: '<p>Hello</p>', templateRenderer: null };", "x");

            // verify the existing property is replaced rather than duplicated
            Assert.DoesNotContain("templateRenderer: null", result.Content);
            Assert.Single(Regex.Matches(result.Content, "templateRenderer:"));
        }

        [Fact]
        public void CssTraversalErrorUsesGenericResourceWording() {
            // traverse above the module root through the shared URL normalizer
            var exception = Assert.Throws<InvalidOperationException>(() => new ModuleFileCompilerCss().Compile(CreateContext("site.css"), ".x { background: url('../image.png'); }"));

            Assert.Contains("Resource reference '../image.png'", exception.Message);
            Assert.DoesNotContain("CSS", exception.Message);
        }

        [Fact]
        public void CssAndHtmlUrlNormalizationStillUseModuleRelativePaths() {
            // exercise both callers of the shared URL normalizer
            var context = CreateContext("components/example.asset");
            var css = new ModuleFileCompilerCss().Compile(context, ".x { background: url('../image.png'); }");
            var html = new ModuleFileCompilerHtml().Compile(context, "<img src='../image.png'>");

            Assert.Contains("url('/image.png')", css.Content);
            Assert.Contains("src='/image.png'", html.Content);
        }

        // methods (private)
        private static ModuleFileCompiler.FileContent Compile(string source, string renderEngine = "html") {
            return new ModuleFileCompilerJs().Compile(CreateContext("components/example.js", renderEngine), source);
        }
        private static ModuleFileCompilerContext CreateContext(string relativePath, string renderEngine = "html") {
            var modulePath = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Tests", "module");
            var config = new ModuleFileCompiler.Config {
                Modules = new Dictionary<string, ModuleFileCompiler.ModuleConfig> {
                    ["test"] = new() { Defaults = new() { Page = new() { RenderEngine = renderEngine } } }
                }
            };
            return new ModuleFileCompilerContext(config, "test", modulePath, Path.Combine(modulePath, relativePath));
        }
        private static string ReadProperty(string source, string property) {
            // capture the JSON string literal emitted for the selected property
            var match = Regex.Match(source, $"""\b{Regex.Escape(property)}\s*:\s*(?<value>"(?:\\.|[^"\\])*")""");
            Assert.True(match.Success, $"Property '{property}' was not emitted as a JavaScript string: {source}");
            return JsonSerializer.Deserialize<string>(match.Groups["value"].Value)!;
        }
    }
}
