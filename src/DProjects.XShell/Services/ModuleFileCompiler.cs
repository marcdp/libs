using System.Text.Json;

using DProjects.Utils;

namespace DProjects.XShell.Services {
    public sealed class ModuleFileCompiler {


        // inner class
        public sealed class Config {
            public Dictionary<string, ModuleConfig> Modules { get; init; } = new();
        }
        public sealed class ModuleConfig {
            public JsonElement ConfigUrl { get; init; }
            public string Version { get; init; } = "";
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
        public Task<FileContent> CompileAsync(string modulePath, string filePath) {
            return CompileAsync(modulePath, filePath, CancellationToken.None);
        }
        public async Task<FileContent> CompileAsync(string modulePath, string filePath, CancellationToken cancellationToken) {
            // read module config
            var moduleDescriptorPath = Path.GetFullPath(modulePath);
            var config = await ReadConfigAsync(moduleDescriptorPath, cancellationToken);
            var mimeType = MimeTypeUtils.GetMimeType(Path.GetExtension(filePath));
            var moduleId = GetLocalModule(config, moduleDescriptorPath).Key;

            // create the shared compilation context
            var moduleDirectory = Path.GetDirectoryName(moduleDescriptorPath);
            if (moduleDirectory == null) throw new ArgumentException("Module descriptor path must have a containing directory.", nameof(modulePath));
            var context = new ModuleFileCompilerContext(config, moduleId, moduleDirectory, filePath);

            // read content
            var content = await File.ReadAllTextAsync(context.FilePath, cancellationToken);

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

        // methods (private)
        internal static async Task<Config> ReadConfigAsync(string moduleDescriptorPath, CancellationToken cancellationToken) {
            // parse JSONC with the same comments and trailing-comma support used by module descriptors
            var configJson = await File.ReadAllTextAsync(moduleDescriptorPath, cancellationToken);
            using var document = JsonDocument.Parse(configJson, new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("modules", out var modules) ||
                modules.ValueKind != JsonValueKind.Object) {
                throw new InvalidOperationException($"Module configuration '{moduleDescriptorPath}' must contain a modules object.");
            }
            return document.RootElement.Deserialize<Config>(new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new InvalidOperationException($"Module configuration '{moduleDescriptorPath}' is empty.");
        }
        internal static KeyValuePair<string, ModuleConfig> GetLocalModule(Config config, string moduleDescriptorPath) {
            // identify the single definition owned by this descriptor without relying on property order
            var localModules = config.Modules
                .Where(module => module.Value != null && module.Value.ConfigUrl.ValueKind == JsonValueKind.Undefined)
                .ToArray();
            if (localModules.Length != 1) {
                var ids = string.Join(", ", localModules.Select(module => $"'{module.Key}'"));
                var suffix = ids.Length == 0 ? "." : $": {ids}.";
                var message = $"Module configuration '{moduleDescriptorPath}' must contain exactly one local module definition, " +
                    $"but found {localModules.Length}{suffix}";
                throw new InvalidOperationException(message);
            }
            return localModules[0];
        }
    }
}
