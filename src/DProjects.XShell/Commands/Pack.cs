using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Utils;
using DProjects.XShell.Services;

using Microsoft.AspNetCore.Diagnostics;

namespace DProjects.XShell.Commands {


    [Description("Pack an XShell module or the XShell framework")]
    [Example("DProjects.XShell pack --source ./x --output ./dist", "Pack an XShell module")]
    [Example("DProjects.XShell pack --source ./xshell --output ./dist", "Pack the XShell framework")]
    public class Pack(IEnvironment environment) : ICommand {

        // inner classes
        private enum PackageKind {
            Module,
            XShell
        }
        private sealed record PackageInfo(PackageKind Kind, string Id, string Version, string DescriptorPath);

        
        // arguments
        [Flag('s', "Source package directory", "")]
        public string Source { get; init; } = "";
        [Flag('o', "Output directory", "")]
        public string Output { get; init; } = "";
        [Flag('z', "Zip", false)]
        public bool Zip { get; init; } = false;


        // methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            // validations
            if (string.IsNullOrWhiteSpace(Source)) throw new ArgumentException("Source directory is required.");
            if (string.IsNullOrWhiteSpace(Output)) throw new ArgumentException("Output directory is required.");

            // prepare
            var sourcePath = Path.GetFullPath(Source);
            var outputPath = Path.GetFullPath(Output);
            if (!Directory.Exists(sourcePath)) {
                throw new DirectoryNotFoundException($"Package directory not found: {sourcePath}");
            }
            if (IsSameOrInside(outputPath, sourcePath)) {
                throw new InvalidOperationException("Output directory must not be the source package directory or a directory inside it.");
            }
            var package = await GetPackageInfoAsync(sourcePath, cancellationToken);
            ValidatePackageNamePart(package.Id, "Package id");
            ValidatePackageNamePart(package.Version, "Package version");
            Directory.CreateDirectory(outputPath);
            var stagingPath = CreateStagingPath(sourcePath, outputPath);
            Directory.CreateDirectory(stagingPath);

            try {
                // copy
                CopyDirectory(sourcePath, stagingPath, cancellationToken);
                var moduleJson = Path.Combine(stagingPath, "module.json");
                var moduleJsonc = Path.Combine(stagingPath, "module.jsonc");
                if (File.Exists(moduleJsonc)) {
                    File.Move(moduleJsonc, moduleJson, overwrite: true);
                    package = new PackageInfo(package.Kind, package.Id, package.Version, moduleJson);
                }

                // remove a copied inventory before compiling the distributable tree
                var indexPath = Path.Combine(stagingPath, FilesIndexer.ModuleFilesJson);
                if (File.Exists(indexPath)) File.Delete(indexPath);

                // compile all runtime resources against the staged descriptor and paths
                var stagedDescriptorPath = Path.Combine(stagingPath, Path.GetFileName(package.DescriptorPath));
                if (package.Kind == PackageKind.Module) await CompileResourcesAsync(stagingPath, stagedDescriptorPath, cancellationToken);

                // inventory only the final compiled package contents
                var moduleFilesIndexer = new FilesIndexer();
                var moduleFilesJson = await moduleFilesIndexer.CreateJsonAsync(stagingPath, cancellationToken);
                await File.WriteAllTextAsync(indexPath, moduleFilesJson, cancellationToken);

                // emit the requested representation from the same compiled staging tree
                var packagePath = Zip ? 
                    await EmitZipAsync(stagingPath, outputPath, package, moduleFilesJson, cancellationToken) :
                    await EmitExpandedAsync(stagingPath, sourcePath, outputPath, package, cancellationToken);

                // output
                await environment.Out.WriteLineAsync(packagePath);
                
            } finally {
                // cleanup
                if (Directory.Exists(stagingPath)) {
                    Directory.Delete(stagingPath, recursive: true);
                }
            }

            // return
            return 0;
        }


        // private methods
        private static async Task<PackageInfo> GetPackageInfoAsync(string sourcePath, CancellationToken cancellationToken) {
            var moduleJson = Path.Combine(sourcePath, "module.json");
            var moduleJsonc = Path.Combine(sourcePath, "module.jsonc");
            var xshellJson = Path.Combine(sourcePath, "xshell.json");
            var xshellJsonc = Path.Combine(sourcePath, "xshell.jsonc");
            if (File.Exists(moduleJson) && File.Exists(moduleJsonc)) {
                throw new InvalidOperationException("Package source contains both module.json and module.jsonc.");
            }
            if (File.Exists(xshellJson) && File.Exists(xshellJsonc)) {
                throw new InvalidOperationException("Package source contains both xshell.json and xshell.jsonc.");
            }
            var moduleDescriptorPath = File.Exists(moduleJson) ? moduleJson : File.Exists(moduleJsonc) ? moduleJsonc : null;
            var xshellDescriptorPath = File.Exists(xshellJson) ? xshellJson : File.Exists(xshellJsonc) ? xshellJsonc : null;
            if (moduleDescriptorPath != null && xshellDescriptorPath != null) {
                throw new InvalidOperationException("Package source cannot contain both a module descriptor and an XShell descriptor.");
            }
            if (moduleDescriptorPath != null) {
                var (id, version) = await ReadModuleIdentityAsync(moduleDescriptorPath, cancellationToken);
                return new PackageInfo(PackageKind.Module, id, version, moduleDescriptorPath);
            }
            if (xshellDescriptorPath != null) {
                var version = await ReadXShellVersionAsync(xshellDescriptorPath, cancellationToken);
                return new PackageInfo(PackageKind.XShell, "xshell", version, xshellDescriptorPath);
            }
            throw new InvalidOperationException("Package source must contain exactly one supported descriptor: module.json[c] or xshell.json[c].");
        }
        private static async Task<(string Id, string Version)> ReadModuleIdentityAsync(string descriptorPath, CancellationToken cancellationToken) {
            // package identity is the descriptor's single local module definition
            var config = await ModuleFileCompiler.ReadConfigAsync(descriptorPath, cancellationToken);
            var localModule = ModuleFileCompiler.GetLocalModule(config, descriptorPath);
            var id = localModule.Key;
            var version = localModule.Value.Version;
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Module id cannot be empty.");
            if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("Module version cannot be empty.");
            return (id, version);
        }
        private static async Task<string> ReadXShellVersionAsync(string descriptorPath, CancellationToken cancellationToken) {
            // parse the XShell descriptor with the same JSONC behavior as module descriptors
            var configJson = await File.ReadAllTextAsync(descriptorPath, cancellationToken);
            using var document = JsonDocument.Parse(configJson, new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("xshell", out var xshell) ||
                xshell.ValueKind != JsonValueKind.Object || !xshell.TryGetProperty("version", out var versionElement) ||
                versionElement.ValueKind != JsonValueKind.String) {
                throw new InvalidOperationException($"XShell configuration '{descriptorPath}' must contain xshell.version.");
            }
            var version = versionElement.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("XShell version cannot be empty.");
            return version;
        }
        private static async Task CompileResourcesAsync(string stagingPath, string descriptorPath, CancellationToken cancellationToken) {
            // snapshot authored resources so generated JavaScript is not compiled a second time
            var resources = Directory.EnumerateFiles(stagingPath, "*", SearchOption.AllDirectories)
                .Where(file => IsCompiledResourceExtension(Path.GetExtension(file)))
                .OrderBy(file => Path.GetRelativePath(stagingPath, file), StringComparer.Ordinal)
                .ToArray();

            // reject JavaScript and HTML SFC sources that target the same runtime path
            var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var runtimePaths = new Dictionary<string, string>(pathComparer);
            foreach (var resource in resources) {
                var isHtml = Path.GetExtension(resource).Equals(".html", StringComparison.OrdinalIgnoreCase);
                var runtimePath = isHtml ? Path.ChangeExtension(resource, ".js") : resource;
                if (runtimePaths.TryGetValue(runtimePath, out var existingResource)) {
                    var existingRelativePath = Path.GetRelativePath(stagingPath, existingResource);
                    var resourceRelativePath = Path.GetRelativePath(stagingPath, resource);
                    var runtimeRelativePath = Path.GetRelativePath(stagingPath, runtimePath);
                    var message = $"Conflicting module resources '{existingRelativePath}' and '{resourceRelativePath}' " +
                        $"both resolve to '{runtimeRelativePath}'.";
                    throw new InvalidOperationException(message);
                }
                runtimePaths.Add(runtimePath, resource);
            }

            // reuse the development compiler for JavaScript, HTML SFC, and standalone CSS
            var compiler = new ModuleFileCompiler();
            foreach (var resource in resources) {
                cancellationToken.ThrowIfCancellationRequested();
                var compiled = await compiler.CompileAsync(descriptorPath, resource, cancellationToken);
                if (Path.GetExtension(resource).Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                    var jsPath = Path.ChangeExtension(resource, ".js");
                    await File.WriteAllTextAsync(jsPath, compiled.Content, cancellationToken);
                    File.Delete(resource);
                } else {
                    await File.WriteAllTextAsync(resource, compiled.Content, cancellationToken);
                }
            }
        }
        private static async Task<string> EmitZipAsync(string stagingPath, string outputPath, PackageInfo package, string moduleFilesJson, CancellationToken cancellationToken) {
            var hash = await ComputeHashAsync(stagingPath, cancellationToken);
            var packagePath = Path.Combine(outputPath, package.Id, $"{package.Version}.{hash}");
            if (ReusePublishedPackage(packagePath, package.Kind, zip: true)) return packagePath;
            var stagingZipPath = stagingPath + "-zip";
            Directory.CreateDirectory(stagingZipPath);
            try {
                // create the immutable resource archive from the staged package tree
                var zipName = package.Kind == PackageKind.Module ? "module.zip" : "xshell.zip";
                var zipPath = Path.Combine(stagingZipPath, zipName);
                ZipFile.CreateFromDirectory(stagingPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

                // parse the staged descriptor with JSONC semantics
                var descriptorPath = Path.Combine(stagingPath, Path.GetFileName(package.DescriptorPath));
                var json = await File.ReadAllTextAsync(descriptorPath, cancellationToken);
                var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    })?.AsObject()
                    ?? throw new InvalidOperationException($"Invalid {package.Kind} configuration '{descriptorPath}'.");

                // place archive metadata on the package's own configuration object
                if (package.Kind == PackageKind.Module) {
                    var modules = root["modules"] as JsonObject ?? throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain a modules object.");
                    var localModules = modules.Where(item => item.Value is JsonObject module && !module.ContainsKey("configUrl")).ToArray();
                    if (localModules.Length != 1) {
                        throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain exactly one local module definition.");
                    }
                    var localModule = (JsonObject)localModules[0].Value!;
                    localModule["assetsUrl"] = "url:./module.zip";
                    localModule["files"] = JsonNode.Parse(moduleFilesJson);
                } else {
                    var xshell = root["xshell"] as JsonObject ?? throw new InvalidOperationException($"XShell configuration '{descriptorPath}' must contain an xshell object.");
                    xshell["assetsUrl"] = "url:./xshell.zip";
                    xshell["files"] = JsonNode.Parse(moduleFilesJson);
                }

                // write the normalized external descriptor beside the archive
                var descriptorName = package.Kind == PackageKind.Module ? "module.json" : "xshell.jsonc";
                var outputDescriptorPath = Path.Combine(stagingZipPath, descriptorName);
                await File.WriteAllTextAsync(outputDescriptorPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

                // publish the descriptor and archive under the immutable package identity
                var tempPackagePath = Path.Combine(outputPath, $".{package.Id}-{package.Version}-{Guid.NewGuid():N}.tmp");
                try {
                    CopyDirectory(stagingZipPath, tempPackagePath, cancellationToken);
                    Directory.CreateDirectory(Path.GetDirectoryName(packagePath)!);
                    try {
                        Directory.Move(tempPackagePath, packagePath);
                    } catch (IOException) when (Directory.Exists(packagePath)) {
                        // another pack may have published the same identity first
                        if (ReusePublishedPackage(packagePath, package.Kind, zip: true)) return packagePath;
                        throw;
                    }
                    return packagePath;
                } finally {
                    if (Directory.Exists(tempPackagePath)) Directory.Delete(tempPackagePath, recursive: true);
                }
            } finally {
                if (Directory.Exists(stagingZipPath)) Directory.Delete(stagingZipPath, recursive: true);
            }
        }
        private static async Task<string> EmitExpandedAsync(string stagingPath, string sourcePath, string outputPath, PackageInfo package, CancellationToken cancellationToken) {
            // build a complete sibling directory before first publication
            var hash = await ComputeHashAsync(stagingPath, cancellationToken);
            var packagePath = Path.Combine(outputPath, package.Id, $"{package.Version}.{hash}");
            if (IsSameOrInside(sourcePath, packagePath)) {
                throw new InvalidOperationException("Expanded package path must not contain or replace the source module directory.");
            }
            if (ReusePublishedPackage(packagePath, package.Kind, zip: false)) return packagePath;
            var tempPackagePath = Path.Combine(outputPath, $".{package.Id}-{package.Version}-{Guid.NewGuid():N}.tmp");
            try {
                CopyDirectory(stagingPath, tempPackagePath, cancellationToken);
                Directory.CreateDirectory(Path.GetDirectoryName(packagePath)!);
                try {
                    Directory.Move(tempPackagePath, packagePath);
                } catch (IOException) when (Directory.Exists(packagePath)) {
                    // another pack may have published the same identity first
                    if (ReusePublishedPackage(packagePath, package.Kind, zip: false)) return packagePath;
                    throw;
                }
                return packagePath;
            } finally {
                if (Directory.Exists(tempPackagePath)) Directory.Delete(tempPackagePath, recursive: true);
            }
        }
        private static bool ReusePublishedPackage(string packagePath, PackageKind kind, bool zip) {
            if (File.Exists(packagePath)) throw new InvalidOperationException($"Package path is an existing file: {packagePath}");
            if (!Directory.Exists(packagePath)) return false;

            // distinguish the two published representations without relying on ZIP archive bytes
            var hasModuleZip = File.Exists(Path.Combine(packagePath, "module.zip"));
            var hasXShellZip = File.Exists(Path.Combine(packagePath, "xshell.zip"));
            var hasModuleDescriptor = File.Exists(Path.Combine(packagePath, "module.json")) || File.Exists(Path.Combine(packagePath, "module.jsonc"));
            var hasXShellDescriptor = File.Exists(Path.Combine(packagePath, "xshell.json")) || File.Exists(Path.Combine(packagePath, "xshell.jsonc"));
            var hasInventory = File.Exists(Path.Combine(packagePath, FilesIndexer.ModuleFilesJson));
            var hasExpectedZip = kind == PackageKind.Module ? hasModuleZip : hasXShellZip;
            var hasOtherZip = kind == PackageKind.Module ? hasXShellZip : hasModuleZip;
            var hasExpectedDescriptor = kind == PackageKind.Module ? hasModuleDescriptor : hasXShellDescriptor;
            var hasOtherDescriptor = kind == PackageKind.Module ? hasXShellDescriptor : hasModuleDescriptor;
            var hasZipDescriptor = kind == PackageKind.Module ? File.Exists(Path.Combine(packagePath, "module.json")) : File.Exists(Path.Combine(packagePath, "xshell.jsonc"));
            var hasAlternateZipDescriptor = kind == PackageKind.Module ? File.Exists(Path.Combine(packagePath, "module.jsonc")) : File.Exists(Path.Combine(packagePath, "xshell.json"));
            if (zip ? hasExpectedZip && hasZipDescriptor && !hasAlternateZipDescriptor && !hasOtherZip && !hasOtherDescriptor && !hasInventory :
                !hasModuleZip && !hasXShellZip && hasExpectedDescriptor && !hasOtherDescriptor && hasInventory) return true;
            if (hasExpectedDescriptor && !hasOtherDescriptor && zip != (hasModuleZip || hasXShellZip)) {
                throw new InvalidOperationException($"Immutable package identity already exists using another representation: {packagePath}");
            }
            throw new InvalidOperationException($"Existing immutable package has an incomplete or unrecognized representation: {packagePath}");
        }
        private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken) {
            // compute a hash of the directory contents to use in the expanded package path
            using var sha256 = SHA256.Create();
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(f => f)) {
                var relativePath = Path.GetRelativePath(path, file);
                var pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
                sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);
                var fileBytes = File.ReadAllBytes(file);
                sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha256.Hash!).ToLowerInvariant().Substring(0,16);
        }
        private static string CreateStagingPath(string sourcePath, string outputPath) {
            // keep staging outside both the authored module and requested output tree
            var stagingPath = Path.Combine(Path.GetTempPath(), "DProjects.XShell", "pack", Guid.NewGuid().ToString("N"));
            if (IsSameOrInside(stagingPath, sourcePath) || IsSameOrInside(stagingPath, outputPath)) {
                throw new InvalidOperationException("Temporary staging directory must be outside the source and output directories.");
            }
            return stagingPath;
        }
        private static bool IsCompiledResourceExtension(string extension) {
            return extension.Equals(".js", StringComparison.OrdinalIgnoreCase) || extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || extension.Equals(".css", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsSameOrInside(string path, string root) {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            return normalizedPath.Equals(normalizedRoot, comparison) || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison);
        }
        private static void ValidatePackageNamePart(string value, string label) {
            // keep descriptor identity from escaping or corrupting the output filename
            if (value is "." or ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar)) {
                throw new InvalidOperationException($"{label} '{value}' cannot be used in a package path.");
            }
        }
        private static void CopyDirectory(string source, string destination, CancellationToken cancellationToken) {
            Directory.CreateDirectory(destination);

            foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)) {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(source, directory);
                Directory.CreateDirectory(Path.Combine(destination, relativePath));
            }

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)) {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(source, file);
                var targetPath = Path.Combine(destination, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(file, targetPath, overwrite: true);
            }
        }
    }
}
