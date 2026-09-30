using System.IO.Compression;
using System.Security.Cryptography;

using DProjects.Commands;
using DProjects.Commands.Attributes;
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
                throw new DirectoryNotFoundException($"Module directory not found: {sourcePath}");
            }
            if (IsSameOrInside(outputPath, sourcePath)) {
                throw new InvalidOperationException("Output directory must not be the source module directory or a directory inside it.");
            }
            var descriptorPath = GetModuleDescriptorPath(sourcePath);
            var (id, version) = await ReadModuleIdentityAsync(descriptorPath, cancellationToken);
            ValidatePackageNamePart(id, "Module id");
            ValidatePackageNamePart(version, "Module version");
            Directory.CreateDirectory(outputPath);
            var stagingPath = CreateStagingPath(sourcePath, outputPath);
            Directory.CreateDirectory(stagingPath);

            try {
                CopyDirectory(sourcePath, stagingPath, cancellationToken);

                // remove a copied inventory before compiling the distributable tree
                var indexPath = Path.Combine(stagingPath, ModuleFilesIndexer.ModuleFilesJson);
                if (File.Exists(indexPath)) File.Delete(indexPath);

                // compile all runtime resources against the staged descriptor and paths
                var stagedDescriptorPath = Path.Combine(stagingPath, Path.GetFileName(descriptorPath));
                await CompileResourcesAsync(stagingPath, stagedDescriptorPath, cancellationToken);

                // inventory only the final compiled package contents
                var moduleFilesIndexer = new ModuleFilesIndexer();
                var json = await moduleFilesIndexer.CreateJsonAsync(stagingPath, cancellationToken);
                await File.WriteAllTextAsync(indexPath, json, cancellationToken);

                // emit the requested representation from the same compiled staging tree
                var packagePath = Zip ? await EmitZipAsync(stagingPath, outputPath, id, version, cancellationToken) :
                    EmitExpanded(stagingPath, sourcePath, outputPath, id, version, cancellationToken);

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
            // package identity is the descriptor's single local module definition
            var config = await ModuleFileCompiler.ReadConfigAsync(descriptorPath, cancellationToken);
            var localModule = ModuleFileCompiler.GetLocalModule(config, descriptorPath);
            var id = localModule.Key;
            var version = localModule.Value.Version;
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Module id cannot be empty.");
            if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("Module version cannot be empty.");
            return (id, version);
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
        private static async Task<string> EmitZipAsync(
            string stagingPath, string outputPath, string id, string version, CancellationToken cancellationToken) {
            // create the ZIP under a temporary name before exposing its immutable hash name
            var tempZipPath = Path.Combine(outputPath, $".{id}-{version}-{Guid.NewGuid():N}.zip");
            try {
                ZipFile.CreateFromDirectory(stagingPath, tempZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                var packageHash = await ComputeHashAsync(tempZipPath, cancellationToken);
                var packagePath = Path.Combine(outputPath, $"{id}-{version}-{packageHash}.zip");
                if (File.Exists(packagePath)) {
                    File.Delete(tempZipPath);
                } else {
                    File.Move(tempZipPath, packagePath);
                }
                return packagePath;
            } finally {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }
        }
        private static string EmitExpanded(
            string stagingPath, string sourcePath, string outputPath, string id, string version, CancellationToken cancellationToken) {
            // build a complete sibling directory before replacing an older expanded package
            var packagePath = Path.Combine(outputPath, $"{id}-{version}");
            if (IsSameOrInside(sourcePath, packagePath)) {
                throw new InvalidOperationException("Expanded package path must not contain or replace the source module directory.");
            }
            if (File.Exists(packagePath)) throw new InvalidOperationException($"Expanded package path is an existing file: {packagePath}");
            var tempPackagePath = Path.Combine(outputPath, $".{id}-{version}-{Guid.NewGuid():N}.tmp");
            var backupPackagePath = Path.Combine(outputPath, $".{id}-{version}-{Guid.NewGuid():N}.backup");
            try {
                CopyDirectory(stagingPath, tempPackagePath, cancellationToken);
                if (Directory.Exists(packagePath)) Directory.Move(packagePath, backupPackagePath);
                try {
                    Directory.Move(tempPackagePath, packagePath);
                } catch {
                    if (Directory.Exists(backupPackagePath) && !Directory.Exists(packagePath)) Directory.Move(backupPackagePath, packagePath);
                    throw;
                }
                if (Directory.Exists(backupPackagePath)) Directory.Delete(backupPackagePath, recursive: true);
                return packagePath;
            } finally {
                if (Directory.Exists(tempPackagePath)) Directory.Delete(tempPackagePath, recursive: true);
                if (Directory.Exists(backupPackagePath) && !Directory.Exists(packagePath)) Directory.Move(backupPackagePath, packagePath);
            }
        }
        private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken) {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
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
            return extension.Equals(".js", StringComparison.OrdinalIgnoreCase) || extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".css", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsSameOrInside(string path, string root) {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            return normalizedPath.Equals(normalizedRoot, comparison) ||
                normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison);
        }
        private static void ValidatePackageNamePart(string value, string label) {
            // keep descriptor identity from escaping or corrupting the output filename
            if (value is "." or ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains(Path.DirectorySeparatorChar) ||
                value.Contains(Path.AltDirectorySeparatorChar)) {
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
