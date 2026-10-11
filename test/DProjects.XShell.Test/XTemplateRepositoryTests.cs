using System.Reflection;
using System.Text.RegularExpressions;
using DProjects.XShell.Services;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateRepositoryTests {

        // methods 
        [Fact]
        public async Task StaticResourceTemplatesCompile() {
            var resourceDirectory = GetResourceDirectory();
            var errors = new List<string>();

            foreach (var path in Directory.EnumerateFiles(resourceDirectory, "*.html", SearchOption.AllDirectories)) {
                // compile authored HTML modules through the same SFC path used by pack and development hosting
                var moduleDirectory = Path.Combine(resourceDirectory, Path.GetRelativePath(resourceDirectory, path).Split(Path.DirectorySeparatorChar)[0]);
                var descriptor = File.Exists(Path.Combine(moduleDirectory, "module.json")) ? Path.Combine(moduleDirectory, "module.json") :
                    Path.Combine(moduleDirectory, "module.jsonc");
                if (!File.Exists(descriptor)) continue;
                try {
                    _ = await new ModuleFileCompiler().CompileAsync(descriptor, path, TestContext.Current.CancellationToken);
                } catch (Exception exception) {
                    errors.Add($"{Path.GetRelativePath(resourceDirectory, path)}: {exception.Message}");
                }
            }

            Assert.True(errors.Count == 0, "Static XTemplate resource compilation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        [Fact]
        public void XObjectTemplateCompiles() {
            var source = File.ReadAllText(Path.Combine(GetResourceDirectory(), "x", "components", "x-object.js"));
            var compilerType = typeof(XTemplateCompiler).Assembly.GetType("DProjects.XShell.Services.XTemplate.XTemplateJavaScriptCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType, new XTemplateCompiler())!;
            var transform = compilerType.GetMethod("Transform", BindingFlags.Instance | BindingFlags.Public)!;

            var transformed = (string)transform.Invoke(compiler, [source, null])!;

            Assert.Contains("templateRenderer:", transformed, StringComparison.Ordinal);
            Assert.Contains("aria-expanded", transformed, StringComparison.Ordinal);
            Assert.Contains("_inspection", transformed, StringComparison.Ordinal);
        }

        [Fact]
        public void StaticModuleTemplatesCompileWhenTemplatePropertyUsesWhitespaceBeforeTheColon() {
            var compilerType = typeof(XTemplateCompiler).Assembly.GetType("DProjects.XShell.Services.XTemplate.XTemplateJavaScriptCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType, new XTemplateCompiler())!;
            var transform = compilerType.GetMethod("Transform", BindingFlags.Instance | BindingFlags.Public)!;
            const string source = "export default { template : `<div>{{ state.value }}</div>` };";

            var transformed = (string)transform.Invoke(compiler, [source, null])!;

            Assert.Contains("templateRenderer:", transformed, StringComparison.Ordinal);
            Assert.Contains("render:", transformed, StringComparison.Ordinal);
            Assert.Contains("dependencies:[", transformed, StringComparison.Ordinal);
            Assert.Contains("slots:[", transformed, StringComparison.Ordinal);
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
            Assert.Contains("(template, context, templateRenderer)", File.ReadAllText(Path.Combine(renderEngineDirectory, "html.js")), StringComparison.Ordinal);
            Assert.Contains("new renderEngineFactoryCreator(implementation.template, context, implementation.templateRenderer)", File.ReadAllText(Path.Combine(loaderDirectory, "component-js.js")), StringComparison.Ordinal);
            Assert.Contains("new renderEngineFactoryCreator(implementation.template, context, implementation.templateRenderer)", File.ReadAllText(Path.Combine(loaderDirectory, "page-js.js")), StringComparison.Ordinal);
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
            return Path.Combine(projectDirectory!, "Resources", new Extensions.ResourcesConfig().ResourceName);
        }

        private static bool IsVendorPath(string path) => path.Contains("\\vendor\\", StringComparison.OrdinalIgnoreCase) || path.Contains("/vendor/", StringComparison.OrdinalIgnoreCase);
    }
}
