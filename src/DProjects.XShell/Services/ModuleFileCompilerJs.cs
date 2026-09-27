namespace DProjects.XShell.Services {
    public class ModuleFileCompilerJs {
        public string Compile(ModuleFileCompiler.Config config, string js, string filePath) {
            // compile js file
            var usesXTemplate = config?.Modules?.Values.Any(module => module.Defaults.Page.RenderEngine == "x" || module.Defaults.Component.RenderEngine == "x") == true;
            if (usesXTemplate && Path.GetExtension(filePath).Equals(".js", StringComparison.OrdinalIgnoreCase)) {
                js = new ModuleFileCompilerJsXTemplate().Compile(config, js, filePath);
            }
            return js;
        }
    }
}
