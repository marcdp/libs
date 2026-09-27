using System.Text.Json;
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
        public async Task<string> CompileAsync(string modulePath, string filePath) {
            // read content
            var content = FileUtils.ReadTextFile(filePath);
            // read module config
            var configJson = FileUtils.ReadTextFile(modulePath);
            configJson = DProjects.Utils.JsonUtils.RemoveComments(configJson);
            var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            if (config == null) return content;
            // compile the file based on its extension
            if (Path.GetExtension(filePath).Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerHtml().Compile(config, content);
            } else if (Path.GetExtension(filePath).Equals(".md", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerMarkdown().Compile(config, content);
            } else if (Path.GetExtension(filePath).Equals(".css", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerCss().Compile(config, content);
            } else if (Path.GetExtension(filePath).Equals(".js", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerJs().Compile(config, content, filePath);
            };
            // return the compiled js
            return content;
        }
    }
}
