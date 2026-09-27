using System.Text.Json;
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
            // read module config
            var moduleDescriptorPath = Path.GetFullPath(modulePath);
            var configJson = await File.ReadAllTextAsync(moduleDescriptorPath);
            configJson = DProjects.Utils.JsonUtils.RemoveComments(configJson);
            var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            if (config == null) return await File.ReadAllTextAsync(filePath);
            // create the shared compilation context
            var moduleDirectory = Path.GetDirectoryName(moduleDescriptorPath);
            if (moduleDirectory == null) throw new ArgumentException("Module descriptor path must have a containing directory.", nameof(modulePath));
            var context = new ModuleFileCompilerContext(config, moduleDirectory, filePath);
            // read content
            var content = await File.ReadAllTextAsync(context.FilePath);
            // compile the file based on its extension
            if (Path.GetExtension(filePath).Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerHtml().Compile(context, content);
            } else if (Path.GetExtension(filePath).Equals(".md", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerMarkdown().Compile(context, content);
            } else if (Path.GetExtension(filePath).Equals(".css", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerCss().Compile(context, content);
            } else if (Path.GetExtension(filePath).Equals(".js", StringComparison.OrdinalIgnoreCase)) {
                content = new ModuleFileCompilerJs().Compile(context, content);
            };
            // return the compiled js
            return content;
        }
    }
}
