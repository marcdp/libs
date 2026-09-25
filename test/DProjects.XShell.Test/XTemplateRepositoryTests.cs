using System.Reflection;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateRepositoryTests {

        // methods
        [Fact]
        public void StaticModuleTemplatesCompileThroughTheServerCompiler() {
            var resourceDirectory = GetResourceDirectory();
            var compilerType = typeof(XTemplateCompiler).Assembly.GetType("DProjects.XShell.Services.XTemplate.XTemplateJavaScriptCompiler", throwOnError: true)!;
            var compiler = Activator.CreateInstance(compilerType, new XTemplateCompiler())!;
            var transform = compilerType.GetMethod("Transform", BindingFlags.Instance | BindingFlags.Public)!;
            var errors = new List<string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(resourceDirectory, "modules"), "*.js", SearchOption.AllDirectories).Where(path => !path.Contains("\\vendor\\", StringComparison.OrdinalIgnoreCase))) {
                var source = File.ReadAllText(path);
                if (!source.Contains("template:", StringComparison.Ordinal)) continue;
                try {
                    transform.Invoke(compiler, [source]);
                } catch (TargetInvocationException exception) {
                    errors.Add($"{Path.GetRelativePath(resourceDirectory, path)}: {exception.InnerException?.Message ?? exception.Message}");
                }
            }

            Assert.True(errors.Count == 0, "Static XTemplate module compilation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        [Fact]
        public void StaticResourceTemplatesCompile() {
            var resourceDirectory = GetResourceDirectory();
            var errors = new List<string>();

            foreach (var path in Directory.EnumerateFiles(Path.Combine(resourceDirectory, "samples"), "*.html", SearchOption.AllDirectories)) {
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
        public void BrowserRuntimeUsesOnlyPrecompiledRenderers() {
            var runtime = File.ReadAllText(Path.Combine(GetResourceDirectory(), "xshell", "render-engines", "x.js"));

            Assert.Contains("server-precompiled templateRenderer", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("_compileTemplateToJsRecursive", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("new Function", runtime, StringComparison.Ordinal);
            Assert.DoesNotContain("eval", runtime, StringComparison.Ordinal);
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
    }
}
