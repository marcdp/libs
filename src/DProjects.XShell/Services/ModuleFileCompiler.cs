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
        public sealed class FileContent {
            public string Content { get; init; } = "";
            public string ContentType { get; init; } = "";
        }



        // methods
        public async Task<FileContent> CompileAsync(string modulePath, string filePath) {
            // read module config
            var moduleDescriptorPath = Path.GetFullPath(modulePath);
            var configJson = await File.ReadAllTextAsync(moduleDescriptorPath);
            configJson = DProjects.Utils.JsonUtils.RemoveComments(configJson);
            var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var mimeType = MimeTypeUtils.GetMimeType(Path.GetExtension(filePath));
            if (config == null) return new FileContent { ContentType = mimeType, Content = await File.ReadAllTextAsync(filePath) };
            var moduleId = config.Modules.Keys.First();

            // create the shared compilation context
            var moduleDirectory = Path.GetDirectoryName(moduleDescriptorPath);
            if (moduleDirectory == null) throw new ArgumentException("Module descriptor path must have a containing directory.", nameof(modulePath));
            var context = new ModuleFileCompilerContext(config, moduleId, moduleDirectory, filePath);

            // read content
            var content = await File.ReadAllTextAsync(context.FilePath);

            // compile the file based on its type
            if (Path.GetExtension(filePath).Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                return new ModuleFileCompilerHtmlSfc().Compile(context, content);
            } else if (Path.GetExtension(filePath).Equals(".css", StringComparison.OrdinalIgnoreCase)) {
                return new ModuleFileCompilerCss().Compile(context, content);
            } else if (Path.GetExtension(filePath).Equals(".js", StringComparison.OrdinalIgnoreCase)) {
                return new ModuleFileCompilerJs().Compile(context, content);
            };

            // return the compiled resource
            return new FileContent { ContentType = mimeType, Content = content };
        }
    }
}
