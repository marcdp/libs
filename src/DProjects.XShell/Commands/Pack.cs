using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Utils;
using DProjects.XShell.Services;

namespace DProjects.XShell.Commands {


    [Description("Pack an XShell module")]
    [Example("DProjects.XShell pack --source ./modules/x --output ./dist", "Pack an XShell module")]
    public class Pack(IEnvironment environment) : ICommand {

        
        // arguments
        [Flag('s', "Source module directory", "")]
        public string Source { get; init; } = "";
        [Flag('o', "Output directory", "")]
        public string Output { get; init; } = "";


        // methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            int status = 0;

            // validations
            if (string.IsNullOrWhiteSpace(Source)) throw new ArgumentException("Source directory is required.");
            if (string.IsNullOrWhiteSpace(Output)) throw new ArgumentException("Output directory is required.");

            // prepare
            var sourcePath = Path.GetFullPath(Source);
            var outputPath = Path.GetFullPath(Output);
            if (!Directory.Exists(sourcePath)) {
                throw new DirectoryNotFoundException($"Module directory not found: {sourcePath}");
            }
            var descriptorPath = GetModuleDescriptorPath(sourcePath);
            var (id, version) = await ReadModuleIdentityAsync(descriptorPath, cancellationToken);
            Directory.CreateDirectory(outputPath);
            var stagingPath = Path.Combine(Path.GetTempPath(), "DProjects.XShell", "pack", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagingPath);

            try {
                CopyDirectory(sourcePath, stagingPath);

                // create index
                var indexPath = Path.Combine(stagingPath, Services.ModuleFilesIndexer.ModuleFilesJson);
                if (File.Exists(indexPath)) File.Delete(indexPath);
                var moduleFilesIndexer = new ModuleFilesIndexer();
                var json = await moduleFilesIndexer.CreateJsonAsync(stagingPath);
                FileUtils.WriteTextFile (indexPath, json);
                var tempZipPath = Path.Combine(outputPath, $".{id}-{version}-{Guid.NewGuid():N}.zip");

                // zip
                ZipFile.CreateFromDirectory(stagingPath, tempZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                
                // compute hash
                var packageHash = await ComputeHashAsync(tempZipPath, cancellationToken);
                var packagePath = Path.Combine(outputPath, $"{id}-{version}-{packageHash}.zip");

                // move to final destination
                if (File.Exists(packagePath)) {
                    File.Delete(tempZipPath);
                } else {
                    File.Move(tempZipPath, packagePath);
                }

                // output
                await environment.Out.WriteLineAsync(packagePath);
                
            } finally {
                // cleanup
                if (Directory.Exists(stagingPath)) {
                    Directory.Delete(stagingPath, recursive: true);
                }
            }

            // return
            return status;
        }


        // private methods
        private static string GetModuleDescriptorPath(string sourcePath) {
            var moduleJson = Path.Combine(sourcePath, "module.json");
            var moduleJsonc = Path.Combine(sourcePath, "module.jsonc");
            if (File.Exists(moduleJson) && File.Exists(moduleJsonc)) {
                throw new InvalidOperationException(
                    "Module directory contains both module.json and module.jsonc.");
            }
            if (File.Exists(moduleJson)) return moduleJson;
            if (File.Exists(moduleJsonc)) return moduleJsonc;
            throw new InvalidOperationException("Module directory must contain module.json or module.jsonc.");
        }
        private static async Task<(string Id, string Version)> ReadModuleIdentityAsync(string descriptorPath, CancellationToken cancellationToken) {
            var text = await File.ReadAllTextAsync(descriptorPath, cancellationToken);

            using var document = JsonDocument.Parse(text, new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var idProperty)) {
                throw new InvalidOperationException("Module descriptor does not define 'id'.");
            }

            if (!root.TryGetProperty("version", out var versionProperty)) {
                throw new InvalidOperationException("Module descriptor does not define 'version'.");
            }

            var id = idProperty.GetString();
            var version = versionProperty.GetString();

            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Module id cannot be empty.");
            if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("Module version cannot be empty.");

            return (id, version);
        }
        private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken) {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        private static void CopyDirectory(string source, string destination) {
            Directory.CreateDirectory(destination);

            foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(source, directory);
                Directory.CreateDirectory(Path.Combine(destination, relativePath));
            }

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(source, file);
                var targetPath = Path.Combine(destination, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, overwrite: true);
            }
        }
    }
}