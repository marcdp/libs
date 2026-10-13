using System.Text.Json;
using System.Text.Json.Nodes;

namespace DProjects.XShell.Services {

    internal enum DescriptorKind { Module, XShell }

    internal sealed class DescriptorInventory {

        // consts
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // methods
        public async Task<string> CreateJsonAsync(string descriptorPath, DescriptorKind kind, CancellationToken cancellationToken) {
            // parse the authored JSON or JSONC descriptor
            var root = JsonNode.Parse(await File.ReadAllTextAsync(descriptorPath, cancellationToken), documentOptions: new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) as JsonObject ?? throw new InvalidOperationException($"Invalid configuration '{descriptorPath}'.");
            var directory = Path.GetDirectoryName(descriptorPath) ?? throw new InvalidOperationException($"Descriptor '{descriptorPath}' has no containing directory.");

            // replace authored inventory metadata with the current physical inventory
            var inventory = CreateJsonArray(await new FilesIndexer().CreateAsync(directory, cancellationToken));
            if (kind == DescriptorKind.Module) {
                var modules = root["modules"] as JsonObject ??
                    throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain a modules object.");
                var locals = modules.Where(item => item.Value is JsonObject value && !value.ContainsKey("configUrl")).ToArray();
                if (locals.Length != 1) {
                    throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain exactly one local module definition, but found {locals.Length}.");
                }
                ((JsonObject)locals[0].Value!)["files"] = inventory;
            } else {
                var xshell = root["xshell"] as JsonObject ?? throw new InvalidOperationException($"XShell configuration '{descriptorPath}' must contain xshell.");
                xshell["files"] = inventory;
            }
            return root.ToJsonString(JsonOptions);
        }
        public static JsonArray CreateJsonArray(IEnumerable<FileIndexItem> files) {
            // keep the structured inventory authoritative until descriptor serialization
            return new JsonArray(files.Select(file => (JsonNode?)new JsonObject {
                ["path"] = file.Path,
                ["size"] = file.Size,
                ["hash"] = file.Hash
            }).ToArray());
        }
    }
}
