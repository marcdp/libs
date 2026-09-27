namespace DProjects.XShell.Services {
    public class ModuleFileCompilerJsXTemplate {
        public string Compile(ModuleFileCompiler.Config config, string js, string filePath) {
            // compile js file with x template
            return new Services.XTemplate.XTemplateJavaScriptCompiler(new Services.XTemplate.XTemplateCompiler()).Transform(js);
        }
    }
}
