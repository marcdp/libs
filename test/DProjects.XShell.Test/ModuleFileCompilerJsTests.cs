using DProjects.XShell.Services;
using DProjects.XShell.Services.XTemplate;

using Xunit;

namespace DProjects.XShell.Test {

    public sealed class ModuleFileCompilerJsTests {

        // methods
        [Fact]
        public void Compile_MetaRenderEngineX_OverridesModuleDefault() {
            var result = Compile("export default { meta: { renderEngine: \"x\" }, template: `<p>content</p>` };", "other");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_WithoutMeta_UsesModuleDefault() {
            var result = Compile("export default { template: `<p>content</p>` };", "x");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_WithoutMetaRenderEngine_UsesModuleDefault() {
            var result = Compile("export default { meta: { stateEngine: \"proxy\" }, template: `<p>content</p>` };", "x");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_QuotedPropertyNames_ReadsRenderEngine() {
            var result = Compile("export default { \"meta\": { \"renderEngine\": \"x\" }, template: `<p>content</p>` };", "other");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_NestedUnrelatedMeta_IgnoresNestedProperty() {
            var result = Compile("export default { something: { meta: { renderEngine: \"wrong\" } }, meta: { renderEngine: \"x\" }, template: `<p>content</p>` };", "other");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_ExportDefaultInsideString_IgnoresFalseMatch() {
            var source = "const text = \"export default { meta: { renderEngine: 'x' } }\"; export default { meta: { renderEngine: \"other\" } };";

            var result = Compile(source, "other");

            Assert.Equal(source, result.Content);
        }
        [Fact]
        public void Compile_ExportDefaultInsideLineComment_IgnoresFalseMatch() {
            var source = "// export default { meta: { renderEngine: \"x\" } }\nexport default { meta: { renderEngine: \"other\" } };";

            var result = Compile(source, "other");

            Assert.Equal(source, result.Content);
        }
        [Fact]
        public void Compile_ExportDefaultInsideBlockComment_IgnoresFalseMatch() {
            var source = "/* export default { meta: { renderEngine: \"x\" } } */ export default { meta: { renderEngine: \"other\" } };";

            var result = Compile(source, "other");

            Assert.Equal(source, result.Content);
        }
        [Fact]
        public void Compile_ExportDefaultInsideTemplateLiteral_IgnoresFalseMatch() {
            var source = "const text = `export default { meta: { renderEngine: \"x\" } }`; export default { meta: { renderEngine: \"other\" } };";

            var result = Compile(source, "other");

            Assert.Equal(source, result.Content);
        }
        [Fact]
        public void Compile_SingleQuotedRenderEngine_ReadsStaticString() {
            var result = Compile("export default { meta: { renderEngine: 'x' }, template: `<p>content</p>` };", "other");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Fact]
        public void Compile_DoubleQuotedRenderEngine_ReadsStaticString() {
            var result = Compile("export default { meta: { renderEngine: \"x\" }, template: `<p>content</p>` };", "other");

            Assert.Contains("templateRenderer: {", result.Content);
        }
        [Theory]
        [InlineData("\".x { background: url('./image.png'); }\"")]
        [InlineData("'.x { background: url(\"./image.png\"); }'")]
        [InlineData("`.x { background: url('./image.png'); }`")]
        public void Compile_StaticCssForms_ReinsertAsStaticTemplateLiteral(string style) {
            var result = Compile($"export default {{ style: {style} }};", "html");

            Assert.Contains("/image.png", ReadProperty(result.Content, "style"));
            Assert.StartsWith("export default", result.Content);
        }
        [Fact]
        public void Compile_StyleResource_RejectsConfigurationOnlyUrlScheme() {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                Compile("export default { style: `a{background:url(url:./image.png)}` };", "html"));

            Assert.Contains("'url:' scheme is not supported", exception.Message);
        }
        [Fact]
        public void Compile_TemplateResource_RejectsConfigurationOnlyUrlScheme() {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                Compile("export default { template: `<img src=\"url:./image.png\">` };", "html"));

            Assert.Contains("'url:' scheme is not supported", exception.Message);
        }
        [Theory]
        [InlineData("\"<img src='./image.png'>\"")]
        [InlineData("'<img src=\"./image.png\">'")]
        [InlineData("`<img src='./image.png'>`")]
        public void Compile_StaticHtmlForms_ReinsertAsStaticTemplateLiteral(string template) {
            var result = Compile($"export default {{ template: {template} }};", "html");

            Assert.Contains("/image.png", ReadProperty(result.Content, "template"));
        }
        [Fact]
        public void Compile_ProcessedCss_EscapesTemplateLiteralSyntaxAndRoundTrips() {
            const string source = "export default { style: \".x::before { content: \\\"C:\\\\assets\\\\image.png ${value} `\\\"; }\\n.y { background: url('./image.png'); }\" };";
            var result = Compile(source, "html");
            var style = ReadProperty(result.Content, "style");

            Assert.Contains("content: \"C:\\assets\\image.png ${value} `\";", style);
            Assert.Contains('\n', style);
            Assert.Contains("/image.png", style);
            Assert.Contains("\\`", result.Content);
            Assert.Contains("\\${value}", result.Content);
            Assert.Contains("C:\\\\assets", result.Content);
        }
        [Fact]
        public void Compile_ProcessedHtml_EscapesTemplateLiteralSyntaxAndRoundTrips() {
            const string source = "export default { template: \"<p title=\\\"` ${value} C:\\\\assets\\\\image.png\\\">Hello</p>\" };";
            var result = Compile(source, "html");

            Assert.Equal("<p title=\"` ${value} C:\\assets\\image.png\">Hello</p>", ReadProperty(result.Content, "template"));
            Assert.Contains("\\`", result.Content);
            Assert.Contains("\\${value}", result.Content);
        }
        [Fact]
        public void Compile_MultilineCss_PreservesPhysicalFormatting() {
            const string source = "export default { style: `.card {\\n    background: url(\"./image.png\");\\n}` };";
            var result = Compile(source, "html");

            Assert.Contains("`.card {\n", result.Content);
            Assert.Contains("\n    background: url(\"/image.png\");", result.Content);
            Assert.Equal(".card {\n    background: url(\"/image.png\");\n}", ReadProperty(result.Content, "style"));
        }
        [Fact]
        public void Compile_RejectsNonStaticStyleAndTemplateValues() {
            var styleException = Assert.Throws<InvalidOperationException>(() => Compile("export default { style: `color:${value}` };", "html"));
            var templateException = Assert.Throws<InvalidOperationException>(() => Compile("export default { template: `<p>${value}</p>` };", "html"));

            Assert.Equal("'style' must be a static string.", styleException.Message);
            Assert.Equal("'template' must be a static string.", templateException.Message);
        }
        [Fact]
        public void HtmlSfc_UsesSafeStaticTemplateLiteralEncoding() {
            const string html = "<template><p title=\"` ${value} C:\\assets\\image.png\">Hello</p></template>";
            var result = new ModuleFileCompilerHtmlSfc().Compile(CreateContext("components/example.html", "html"), html);

            Assert.Equal("<p title=\"` ${value} C:\\assets\\image.png\">Hello</p>", ReadProperty(result.Content, "template"));
            Assert.Contains("\\`", result.Content);
            Assert.Contains("\\${value}", result.Content);
        }
        [Fact]
        public void Compile_NonStaticRenderEngine_ThrowsClearError() {
            var exception = Assert.Throws<InvalidOperationException>(() => Compile("export default { meta: { renderEngine: getRenderEngine() } };", "other"));

            Assert.Contains("'meta.renderEngine' must be a static string", exception.Message);
        }
        [Fact]
        public void Compile_XTemplate_InsertsRendererAndPreservesSurroundingSource() {
            var result = Compile("import value from \"./value.js\";\nexport default { template: `<p>content</p>`, state: { value } };\nconst tail = true;", "x");

            Assert.StartsWith("import value from \"./value.js\";", result.Content);
            Assert.Contains("templateRenderer: {", result.Content);
            Assert.EndsWith("const tail = true;", result.Content);
        }
        [Fact]
        public void Compile_XTemplate_ReplacesExistingRenderer() {
            var result = Compile("export default { template: `<p>content</p>`, templateRenderer: oldRenderer, state: {} };", "x");

            Assert.DoesNotContain("oldRenderer", result.Content);
            Assert.Equal(1, result.Content.Split("templateRenderer:", StringSplitOptions.None).Length - 1);
        }

        // methods (private)
        private static ModuleFileCompiler.FileContent Compile(string source, string moduleRenderEngine) {
            var config = new ModuleFileCompiler.Config {
                Modules = new Dictionary<string, ModuleFileCompiler.ModuleConfig> {
                    ["test"] = new ModuleFileCompiler.ModuleConfig {
                        Defaults = new ModuleFileCompiler.Defaults {
                            Page = new ModuleFileCompiler.PageDefaults { RenderEngine = moduleRenderEngine }
                        }
                    }
                }
            };
            var modulePath = Path.GetFullPath(AppContext.BaseDirectory);
            var context = new ModuleFileCompilerContext(config, "test", modulePath, Path.Combine(modulePath, "page.js"));
            return new ModuleFileCompilerJs().Compile(context, source);
        }
        private static ModuleFileCompilerContext CreateContext(string relativePath, string renderEngine) {
            var config = new ModuleFileCompiler.Config {
                Modules = new Dictionary<string, ModuleFileCompiler.ModuleConfig> {
                    ["test"] = new ModuleFileCompiler.ModuleConfig {
                        Defaults = new ModuleFileCompiler.Defaults {
                            Page = new ModuleFileCompiler.PageDefaults { RenderEngine = renderEngine }
                        }
                    }
                }
            };
            var modulePath = Path.GetFullPath(AppContext.BaseDirectory);
            return new ModuleFileCompilerContext(config, "test", modulePath, Path.Combine(modulePath, relativePath));
        }
        private static string ReadProperty(string source, string property) {
            var value = JavaScriptSource.Parse(source).FindDefaultExportObject()?.FindProperty(property)?.Value.GetStaticString();
            Assert.NotNull(value);
            return value;
        }
    }
}
