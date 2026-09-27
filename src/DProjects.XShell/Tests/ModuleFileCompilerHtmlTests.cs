using DProjects.XShell.Services;

using Xunit;

namespace DProjects.XShell.Tests {

    public sealed class ModuleFileCompilerHtmlTests {

        // methods
        [Fact]
        public void Compile_StyleTemplateAndModuleScript_ProducesCanonicalCompiledJavaScript() {
            var html = """
                <style>
                    .user-card { padding: 1rem; }
                </style>
                <template>
                    <div class="user-card">{{ state.name }}</div>
                </template>
                <script type="module">
                    import helper from "./helper.js";
                    export const contract = { description: "User card" };
                    const value = helper();
                    export default {
                        meta: { renderEngine: "x", stateEngine: "proxy" },
                        state: { value }
                    };
                </script>
                """;

            // compile through both HTML and JavaScript stages
            var result = Compile(html);

            Assert.Equal("application/javascript", result.ContentType);
            Assert.Contains("import helper from \"./helper.js\";", result.Content);
            Assert.Contains("export const contract = { description: \"User card\" };", result.Content);
            Assert.Contains("const value = helper();", result.Content);
            Assert.Contains("style: `", result.Content);
            Assert.Contains(".user-card { padding: 1rem; }", result.Content);
            Assert.Contains("template: `", result.Content);
            Assert.Contains("<div class=\"user-card\">{{ state.name }}</div>", result.Content);
            Assert.Contains("templateRenderer: {render:", result.Content);
        }
        [Fact]
        public void Compile_WithoutStyle_InjectsOnlyTemplate() {
            var result = Compile("""
                <template><p>content</p></template>
                <script type="module">export default { meta: { renderEngine: "x" } };</script>
                """);

            Assert.Contains("template: `<p>content</p>`", result.Content);
            Assert.DoesNotContain("style: `", result.Content);
            Assert.Contains("templateRenderer: {render:", result.Content);
        }
        [Fact]
        public void Compile_WithoutModuleScript_UsesDefaultModuleDefinition() {
            var result = Compile("""
                <template><div>Hello</div></template>
                """);

            Assert.Contains("export default {", result.Content);
            Assert.Contains("template: `<div>Hello</div>`", result.Content);
            Assert.DoesNotContain("style: `", result.Content);
            Assert.Contains("templateRenderer: {render:", result.Content);
        }
        [Fact]
        public void Compile_StyleAndTemplateWithoutModuleScript_InjectsBothProperties() {
            var result = Compile("""
                <style>
                .title { font-weight: bold; }
                </style>
                <template><div class="title">Hello</div></template>
                """);

            Assert.Contains("style: `", result.Content);
            Assert.Contains(".title { font-weight: bold; }", result.Content);
            Assert.Contains("template: `<div class=\"title\">Hello</div>`", result.Content);
            Assert.Contains("templateRenderer: {render:", result.Content);
        }
        [Fact]
        public void Compile_BackticksSubstitutionsAndBackslashes_ProducesStaticTemplateLiterals() {
            var result = Compile("""
                <style>.sample::before { content: "` ${style}"; background: url(C:\assets\image.png); }</style>
                <template><pre>` ${template} C:\templates\sample</pre></template>
                <script type="module">export default {};</script>
                """);

            Assert.Contains(@"content: ""\` \${style}""; background: url(C:\\assets\\image.png);", result.Content);
            Assert.Contains(@"<pre>\` \${template} C:\\templates\\sample</pre>", result.Content);
            Assert.Contains("templateRenderer: {render:", result.Content);
        }
        [Fact]
        public void Compile_NonModuleScriptAndFalseExports_UsesOnlyRealModuleDefaultExport() {
            var result = Compile("""
                <script>const ignored = "export default { template: `ignored` }";</script>
                <template><p>real</p></template>
                <script type="module">
                    // export default { template: `comment` };
                    const text = "export default { template: `string` }";
                    export default { meta: { renderEngine: "x" } };
                </script>
                """);

            Assert.Contains("const text = \"export default { template: `string` }\";", result.Content);
            Assert.Contains("template: `<p>real</p>`", result.Content);
            Assert.DoesNotContain("ignored", result.Content);
        }
        [Theory]
        [InlineData("<template>a</template><template>b</template><script type=\"module\">export default {};</script>", "multiple <template>")]
        [InlineData("<style>a</style><style>b</style><template>x</template><script type=\"module\">export default {};</script>", "multiple <style>")]
        [InlineData("<template>x</template><script type=\"module\">export default {};</script><script type=\"module\">export default {};</script>", "multiple <script type=\"module\">")]
        [InlineData("<script type=\"module\">export default {};</script>", "exactly one <template>")]
        public void Compile_InvalidSectionCounts_Throws(string html, string message) {
            var exception = Assert.Throws<InvalidOperationException>(() => Compile(html));

            Assert.Contains(message, exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Theory]
        [InlineData("", "must contain a default export")]
        [InlineData("const value = 1;", "must contain a default export")]
        [InlineData("export default createDefinition();", "must be an object literal")]
        public void Compile_InvalidDefaultExport_Throws(string moduleScript, string message) {
            var html = $"<template><p>content</p></template><script type=\"module\">{moduleScript}</script>";

            var exception = Assert.Throws<InvalidOperationException>(() => Compile(html));

            Assert.Contains(message, exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Theory]
        [InlineData("style: `existing`")]
        [InlineData("template: `existing`")]
        [InlineData("\"style\": `existing`")]
        [InlineData("[\"template\"]: `existing`")]
        [InlineData("style")]
        public void Compile_ExistingHtmlOwnedProperty_Throws(string property) {
            var html = $"<template><p>content</p></template><script type=\"module\">const style = 'x'; export default {{ {property} }};</script>";

            var exception = Assert.Throws<InvalidOperationException>(() => Compile(html));

            Assert.Contains("already declares", exception.Message);
        }

        // methods (private)
        private static ModuleFileCompiler.FileContent Compile(string html) {
            var config = new ModuleFileCompiler.Config {
                Modules = new Dictionary<string, ModuleFileCompiler.ModuleConfig> {
                    ["test"] = new ModuleFileCompiler.ModuleConfig {
                        Defaults = new ModuleFileCompiler.Defaults {
                            Page = new ModuleFileCompiler.PageDefaults { RenderEngine = "x" }
                        }
                    }
                }
            };
            var modulePath = Path.GetFullPath(AppContext.BaseDirectory);
            var context = new ModuleFileCompilerContext(config, modulePath, Path.Combine(modulePath, "page.html"));
            return new ModuleFileCompilerHtml().Compile(context, html);
        }
    }
}
