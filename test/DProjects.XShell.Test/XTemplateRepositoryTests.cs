using System.Reflection;
using System.Text.RegularExpressions;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateRepositoryTests {

        // methods 
        [Fact]
        public void StaticResourceTemplatesCompile() {
            var resourceDirectory = GetResourceDirectory();
            var errors = new List<string>();

            foreach (var path in Directory.EnumerateFiles(resourceDirectory, "*.html", SearchOption.AllDirectories)) {
                var template = File.ReadAllText(path);
                if (!template.Contains("x-", StringComparison.Ordinal) && !template.Contains("{{", StringComparison.Ordinal)) continue;
                try {
                    _ = new XTemplateCompiler().Compile(template);
                } catch (Exception exception) {
                    errors.Add($"{Path.GetRelativePath(resourceDirectory, path)}: {exception.Message}");
                }
            }

            Assert.True(errors.Count == 0, "Static XTemplate resource compilation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        [Fact]
        public void StaticModuleTemplatesCompileWhenTemplatePropertyUsesWhitespaceBeforeTheColon() {
            var compilerType = typeof(XTemplateCompiler).Assembly.GetType("DProjects.XShell.Services.XTemplate.XTemplateJavaScriptCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType, new XTemplateCompiler())!;
            var transform = compilerType.GetMethod("Transform", BindingFlags.Instance | BindingFlags.Public)!;
            const string source = "export default { template : `<div>{{ state.value }}</div>` };";

            var transformed = (string)transform.Invoke(compiler, [source])!;

            Assert.Contains("templateRenderer:", transformed, StringComparison.Ordinal);
            Assert.Contains("{render:", transformed, StringComparison.Ordinal);
            Assert.Contains("dependencies:[", transformed, StringComparison.Ordinal);
            Assert.Contains("slots:[", transformed, StringComparison.Ordinal);
        }

        [Fact]
        public void StaticModuleXTemplatesCompileToCompleteArtifacts() {
            var resourceDirectory = GetResourceDirectory();
            var compilerType = typeof(XTemplateCompiler).Assembly.GetType("DProjects.XShell.Services.XTemplate.XTemplateJavaScriptCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType, new XTemplateCompiler())!;
            var transform = compilerType.GetMethod("Transform", BindingFlags.Instance | BindingFlags.Public)!;
            var errors = new List<string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(resourceDirectory, "modules"), "*.js", SearchOption.AllDirectories)) {
                var source = File.ReadAllText(path);
                if (!Regex.IsMatch(source, @"\btemplate\s*:")) continue;
                try {
                    var transformed = (string)transform.Invoke(compiler, [source])!;
                    if (!transformed.Contains("templateRenderer:", StringComparison.Ordinal)) errors.Add($"{Path.GetRelativePath(resourceDirectory, path)}: no compiled artifact was emitted.");
                } catch (TargetInvocationException exception) when (exception.InnerException != null) {
                    errors.Add($"{Path.GetRelativePath(resourceDirectory, path)}: {exception.InnerException.Message}");
                }
            }

            Assert.True(errors.Count == 0, "Static module XTemplate compilation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        [Fact]
        public void ClientRuntimeUsesOnlyPrecompiledRenderers() {
            var runtime = File.ReadAllText(Path.Combine(GetResourceDirectory(), "xshell", "render-engines", "x.js"));

            Assert.Contains("server-precompiled templateRenderer", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("_compileTemplateToJsRecursive", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("new Function", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("eval", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("innerHTML = template", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("querySelectorAll(\"*\")", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("rewriteDocumentUrls(template", runtime, StringComparison.Ordinal);
        }

        [Fact]
        public void RenderEnginesKeepTheCommonThreeArgumentFactoryContract() {
            var renderEngineDirectory = Path.Combine(GetResourceDirectory(), "xshell", "render-engines");
            var loaderDirectory = Path.Combine(GetResourceDirectory(), "xshell", "loaders");

            Assert.Contains("(template, context, templateRenderer)", File.ReadAllText(Path.Combine(renderEngineDirectory, "x.js")), StringComparison.Ordinal);
            Assert.Contains("(template, context, templateRenderer)", File.ReadAllText(Path.Combine(renderEngineDirectory, "plain.js")), StringComparison.Ordinal);
            Assert.Contains("(template, context, templateRenderer)", File.ReadAllText(Path.Combine(renderEngineDirectory, "markdown.js")), StringComparison.Ordinal);
            Assert.Contains("new renderEngineFactoryCreator(definition.template, context, definition.templateRenderer)", File.ReadAllText(Path.Combine(loaderDirectory, "component-js.js")), StringComparison.Ordinal);
            Assert.Contains("new renderEngineFactoryCreator(definition.template, context, templateRenderer)", File.ReadAllText(Path.Combine(loaderDirectory, "page-js.js")), StringComparison.Ordinal);
        }

        [Fact]
        public void ComponentAndPageSlotValidationDoNotParseRawTemplateSource() {
            var loaderDirectory = Path.Combine(GetResourceDirectory(), "xshell", "loaders");
            var component = File.ReadAllText(Path.Combine(loaderDirectory, "component-js.js"));
            var page = File.ReadAllText(Path.Combine(loaderDirectory, "page-js.js"));

            Assert.DoesNotContain("slotRegex", component, StringComparison.Ordinal);
            Assert.DoesNotContain("nameRegex", component, StringComparison.Ordinal);
            Assert.DoesNotContain("definition.template.matchAll", component, StringComparison.Ordinal);
            Assert.DoesNotContain("slotRegex", page, StringComparison.Ordinal);
            Assert.DoesNotContain("definition.template.matchAll", page, StringComparison.Ordinal);
            Assert.Contains("renderEngineFactory.slots", component, StringComparison.Ordinal);
            Assert.Contains("renderEngineFactory.slots", page, StringComparison.Ordinal);
        }

        [Fact]
        public void TransformerPredicatesAreAcceptedWithoutAllowingHostMethods() {
            var javascript = new XTemplateCompiler().Compile("<span x-if=\"state.type | endsWith('_i18n')\"></span>");

            Assert.Contains("utils.expr.transform(utils.expr.member(state, \"type\"), \"endsWith\", () => [\"_i18n\"], i18n)", javascript, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() => new XTemplateCompiler().Compile("<span x-if=\"state.type.endsWith('_i18n')\"></span>"));
            Assert.Throws<InvalidOperationException>(() => new XTemplateCompiler().Compile("<span x-if=\"endsWith(state.type, '_i18n')\"></span>"));
        }

        // methods (private)
        private static string GetResourceDirectory() {
            var projectDirectory = typeof(Extensions).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "ProjectDirectory").Value;
            return Path.Combine(projectDirectory!, "Resources", Extensions.ResourceName);
        }

        private static bool IsVendorPath(string path) => path.Contains("\\vendor\\", StringComparison.OrdinalIgnoreCase) || path.Contains("/vendor/", StringComparison.OrdinalIgnoreCase);
    }
}
