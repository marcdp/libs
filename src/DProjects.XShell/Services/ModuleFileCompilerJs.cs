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

            // compile style urls
            var styleProperty = document.FindDefaultExportObject()?.FindProperty("style");
            if (styleProperty != null) {
                // compile CSS and inject into JS
                var style = styleProperty.Value.GetStaticString();
                if (style == null) throw new InvalidOperationException("'style' must be a static string.");
                var contextToUse = new ModuleFileCompilerContext(context.Config, context.ModuleId, context.ModulePath, context.FilePath + ".css");
                var styleProcessed = new ModuleFileCompilerCss().Compile(contextToUse, style);
                // replace old style property with new one
                var styleJs = XTemplate.JavaScriptSource.EncodeStaticTemplateLiteral(styleProcessed.Content);
                js = js[..styleProperty.Value.Start] + styleJs + js[styleProperty.Value.End..];
                // reparse because offsets in 'document' refer to the old source
                document = XTemplate.JavaScriptSource.Parse(js);
            }

            // compile template urls
            var templateProperty = document.FindDefaultExportObject()?.FindProperty("template");
            if (templateProperty != null) {
                // compile HTML and inject into JS
                var template = templateProperty.Value.GetStaticString();
                if (template == null) throw new InvalidOperationException("'template' must be a static string.");
                var contextToUse = new ModuleFileCompilerContext(context.Config, context.ModuleId, context.ModulePath, context.FilePath + ".html");
                var templateProcessed = new ModuleFileCompilerHtml().Compile(contextToUse, template);
                // replace old template property with new one
                var templateJs = XTemplate.JavaScriptSource.EncodeStaticTemplateLiteral(templateProcessed.Content);
                js = js[..templateProperty.Value.Start] + templateJs + js[templateProperty.Value.End..];
                // reparse because offsets in 'document' refer to the old source
                document = XTemplate.JavaScriptSource.Parse(js);
            }

            // compile static X Templates without evaluating the module
            if (pageRenderEngine == "x") {
                js = new XTemplate.XTemplateJavaScriptCompiler(new XTemplate.XTemplateCompiler()).Transform(js);
            }

            // return
            return new ModuleFileCompiler.FileContent { ContentType = "application/javascript", Content = js };
        }
    }
}
