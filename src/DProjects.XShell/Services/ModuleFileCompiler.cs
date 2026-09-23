using System.Text.Json;
using System.Security.Cryptography;

using DProjects.Utils;

namespace DProjects.XShell.Services {

    public sealed class ModuleFileCompiler {


        // inner class
        public sealed class Config {
            public Dictionary<string, ModuleConfig> Modules { get; init; } = new();
        }
        public sealed class ModuleConfig {
            public Defaults Defaults { get; init; } = new();
        }
        public sealed class PageDefaults {
            public string RenderEngine { get; init; } = "";
        }
        public sealed class ComponentDefaults {
            public string RenderEngine { get; init; } = "";
        }
        public sealed class Defaults {
            public PageDefaults Page { get; init; } = new();
            public ComponentDefaults Component { get; init; } = new();
        }


        // methods
        public async Task<string> CompileToJsAsync(string modulePath, string jsPath) {
            // load module
            var moduleJson = FileUtils.ReadTextFile(modulePath);
            moduleJson = DProjects.Utils.JsonUtils.RemoveComments(moduleJson);
            var config = JsonSerializer.Deserialize<Config>(moduleJson, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var content = FileUtils.ReadTextFile(jsPath);
            // check the default render engine
            var renderEngine = config?.Modules?.Values.First().Defaults.Page.RenderEngine;
            if (renderEngine == "x") {
                content = CompileXTemplate(content);
            }
            // return the compiled js
            return content;
        }


        // private methods
        private string CompileHtml(string html) {
            // TODO ...
            throw new NotImplementedException();
        }
        private string CompileMarkdown(string html) {
            // TODO ...
            throw new NotImplementedException();
        }
        private string CompileXTemplate(string js) {
            // TODO ...

            return js;
        }

    }
}