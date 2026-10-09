namespace DProjects.XShell.Services {

    internal class ModuleFileCompilerJs {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string js) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(js);

            // resolve the resource render engine from static metadata or its module default
            var moduleConfig = context.Config.Modules[context.ModuleId];
            var isComponent = context.RelativePath.StartsWith("/components/", StringComparison.OrdinalIgnoreCase);
            var defaultRenderEngine = isComponent ? moduleConfig.Defaults.Component.RenderEngine : moduleConfig.Defaults.Page.RenderEngine;
            var document = XTemplate.JavaScriptSource.Parse(js);
            var renderEngineProperty = document.FindDefaultExportObject()?.FindProperty("meta")?.Value.AsObject()?.FindProperty("renderEngine");
            var renderEngine = defaultRenderEngine;
            if (renderEngineProperty != null) {
                renderEngine = renderEngineProperty.Value.GetStaticString();
                if (renderEngine == null) throw new InvalidOperationException("'meta.renderEngine' must be a static string.");
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
            if (renderEngine == "x") {
                var currentComponentName = ResolveCurrentComponentName(context, document);
                js = new XTemplate.XTemplateJavaScriptCompiler(new XTemplate.XTemplateCompiler()).Transform(js, currentComponentName);
            }

            // return
            return new ModuleFileCompiler.FileContent { ContentType = "application/javascript", Content = js };
        }

        // methods (private)
        private static string? ResolveCurrentComponentName(ModuleFileCompilerContext context, XTemplate.JavaScriptSource document) {
            // use the module resource convention to keep page templates independent from component identity
            if (!context.RelativePath.StartsWith("/components/", StringComparison.OrdinalIgnoreCase)) return null;

            var componentId = document.FindDefaultExportObject()?.FindProperty("meta")?.Value.AsObject()?.FindProperty("id");
            if (componentId != null) {
                return componentId.Value.GetStaticString() ?? throw new InvalidOperationException("'meta.id' must be a static string.");
            }
            return Path.GetFileNameWithoutExtension(context.FilePath);
        }
    }
}
