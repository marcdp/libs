namespace DProjects.XShell.Services {

    public class ModuleFileCompilerJsXTemplate {

        // methods
        public string Compile(ModuleFileCompilerContext context, string js) {
            // compile js file with x template
            return new Services.XTemplate.XTemplateJavaScriptCompiler(new Services.XTemplate.XTemplateCompiler()).Transform(js);
        }
    }
}
