namespace DProjects.XShell.Services {

    public class ModuleFileCompilerJs {

        // methods
        public string Compile(ModuleFileCompilerContext context, string js) {
            // compile js file
            var usesXTemplate = context.Config.Modules?.Values.Any(module => module.Defaults.Page.RenderEngine == "x" || module.Defaults.Component.RenderEngine == "x") == true;
            if (usesXTemplate && Path.GetExtension(context.FilePath).Equals(".js", StringComparison.OrdinalIgnoreCase)) {
                js = new Services.XTemplate.XTemplateJavaScriptCompiler(new Services.XTemplate.XTemplateCompiler()).Transform(js);
            }
            return js;
        }
    }
}
