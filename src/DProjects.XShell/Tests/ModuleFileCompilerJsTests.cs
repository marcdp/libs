using DProjects.XShell.Services;

using Xunit;

namespace DProjects.XShell.Tests {

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
    }
}
