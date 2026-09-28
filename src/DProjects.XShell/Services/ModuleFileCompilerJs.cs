namespace DProjects.XShell.Services {

    public class ModuleFileCompilerJs {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string js) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(js);

            // resolve the page render engine from static metadata or the module default
            var moduleConfig = context.Config.Modules[context.ModuleId];
            var moduleRenderEngine = moduleConfig.Defaults.Page.RenderEngine;
            var document = XTemplate.JavaScriptSource.Parse(js);
            var renderEngineProperty = document.FindDefaultExportObject()?.FindProperty("meta")?.Value.AsObject()?.FindProperty("renderEngine");
            var pageRenderEngine = moduleRenderEngine;
            if (renderEngineProperty != null) {
                pageRenderEngine = renderEngineProperty.Value.GetStaticString();
                if (pageRenderEngine == null) throw new InvalidOperationException("'meta.renderEngine' must be a static string.");
            }

            // get style
            var styleProperty = document.FindDefaultExportObject()?.FindProperty("style");
            int kkk = 132;

            // get template
            // ...

            // compile static X Templates without evaluating the module
            if (pageRenderEngine == "x") js = new XTemplate.XTemplateJavaScriptCompiler(new XTemplate.XTemplateCompiler()).Transform(js);
            return new ModuleFileCompiler.FileContent { ContentType = "application/javascript", Content = js };
        }
    }
}
