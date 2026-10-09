using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class PublicApiTests {

        // methods
        [Fact]
        public void ExportedTypes_PreserveHostAndXTemplateApisWithoutImplementationTypes() {
            var assembly = typeof(Extensions).Assembly;
            var exportedNames = assembly.GetExportedTypes().Select(type => type.FullName ?? type.Name).ToHashSet(StringComparer.Ordinal);

            Assert.Null(assembly.GetType("DProjects.XShell.Services.BoostrapFilesBuilder"));

            // keep host and documented renderer extension points available to applications
            Assert.Contains(typeof(Extensions).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.Configuration).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.AppConfig).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.TempConfig).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.XShellConfig).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.ResourcesConfig).FullName, exportedNames);
            Assert.Contains(typeof(Extensions.ServerConfig).FullName, exportedNames);
            Assert.Contains(typeof(Commands.Server).FullName, exportedNames);
            Assert.Contains(typeof(Commands.Pack).FullName, exportedNames);
            Assert.Contains(typeof(Assembly).FullName, exportedNames);
            Assert.Contains(typeof(XTemplateRenderer).FullName, exportedNames);
            Assert.Contains(typeof(XTemplateRendererOptions).FullName, exportedNames);
            Assert.Contains(typeof(IXTemplateObjectAdapter).FullName, exportedNames);
            Assert.Contains(typeof(XTemplateReflectionObjectAdapter).FullName, exportedNames);

            // keep hosting and compiler implementation details out of the exported surface
            foreach (var name in new[] {
                "DProjects.XShell.Services.BoostrapFilesBuilder",
                "DProjects.XShell.Services.BoostrapFilesBuilder",
                "DProjects.XShell.Middlewares.ResourcesMiddleware",
                "DProjects.XShell.Middlewares.TempMiddleware",
                "DProjects.XShell.Services.FilesIndexer",
                "DProjects.XShell.Services.ModuleFileCompiler",
                "DProjects.XShell.Services.ModuleFileCompilerContext",
                "DProjects.XShell.Services.ModuleFileCompilerJs",
                "DProjects.XShell.Services.ModuleFileCompilerCss",
                "DProjects.XShell.Services.ModuleFileCompilerHtml",
                "DProjects.XShell.Services.ModuleFileCompilerHtmlSfc"
            }) {
                Assert.DoesNotContain(name, exportedNames);
            }
        }
    }
}
