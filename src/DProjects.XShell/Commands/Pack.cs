using System.Buffers.Binary;
using System.IO.Compression;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Utils;
using DProjects.XShell.Services;

namespace DProjects.XShell.Commands {

    [Description("Build and publish a complete XShell application distribution")]
    [Example("DProjects.XShell pack --source ./app --output ./dist", "Package an application and all its dependencies")]
    public class Pack(IEnvironment environment) : ICommand {

        // consts
        private const string ModuleIdPattern = "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$";
        private const string ReleaseFileName = "release.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // inner classes
        private sealed record ModuleNode(string Id, string Version, string DescriptorPath, JsonObject Configuration, List<Reference> References);
        private sealed record Reference(string Id, string DescriptorPath);
        private sealed record BuiltPackage(string Id, string Version, string Hash, string RelativeDirectory, string DescriptorName);

        // props
        [Flag('s', "Root application module directory", "")]
        public string Source { get; init; } = "";
        [Flag('o', "Distribution output directory", "")]
        public string Output { get; init; } = "";

        // methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            // validate paths before touching the destination
            if (string.IsNullOrWhiteSpace(Source)) throw new ArgumentException("Source directory is required.");
            if (string.IsNullOrWhiteSpace(Output)) throw new ArgumentException("Output directory is required.");
            var sourcePath = Path.GetFullPath(Source);
            var outputPath = Path.GetFullPath(Output);
            if (!Directory.Exists(sourcePath)) throw new DirectoryNotFoundException($"Application directory not found: {sourcePath}");
            if (IsSameOrInside(outputPath, sourcePath) || IsSameOrInside(sourcePath, outputPath)) {
                throw new InvalidOperationException("Source and output directories must not contain one another.");
            }

            // discover and validate the complete graph without publishing anything
            var hasModule = File.Exists(Path.Combine(sourcePath, "module.json")) || File.Exists(Path.Combine(sourcePath, "module.jsonc"));
            var hasFramework = File.Exists(Path.Combine(sourcePath, "xshell.json")) || File.Exists(Path.Combine(sourcePath, "xshell.jsonc"));
            if (hasModule && hasFramework) throw new InvalidOperationException("Source cannot contain both a module and an XShell descriptor.");
            var rootDescriptor = FindDescriptor(sourcePath, hasFramework ? "xshell" : "module");
            var frameworkDescriptor = hasFramework ? rootDescriptor : FindBundledFrameworkDescriptor();
            var order = hasFramework ? [] : await DiscoverAsync(rootDescriptor, cancellationToken);

            // build every canonical package in private staging on the output filesystem
            var stagingPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $".{Path.GetFileName(outputPath)}-pack-{Guid.NewGuid():N}.tmp");
            Directory.CreateDirectory(stagingPath);
            try {
                var packages = new Dictionary<string, BuiltPackage>(StringComparer.Ordinal);
                foreach (var node in order) {
                    cancellationToken.ThrowIfCancellationRequested();
                    packages.Add(node.Id, await BuildModuleAsync(node, packages, stagingPath, outputPath, cancellationToken));
                }
                await ValidateBuiltReferencesAsync(order, packages, stagingPath, cancellationToken);
                var framework = await BuildFrameworkAsync(frameworkDescriptor, stagingPath, outputPath, cancellationToken);
                packages.Add(framework.Id, framework);
                var root = hasFramework ? framework : packages[order[^1].Id];
                await PublishAsync(outputPath, stagingPath, root, framework,
                    packages.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray(), cancellationToken);
                await environment.Out.WriteLineAsync(Path.Combine(outputPath, ReleaseFileName));
            } finally {
                if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, recursive: true);
            }
            return 0;
        }

        // methods (private)
        private static async Task<List<ModuleNode>> DiscoverAsync(string rootDescriptor, CancellationToken cancellationToken) {
            // follow local definition documents with the same ownership rules as Bootstrap
            var byPath = new Dictionary<string, ModuleNode>(PathComparer());
            var byId = new Dictionary<string, ModuleNode>(StringComparer.Ordinal);
            var states = new Dictionary<string, int>(PathComparer());
            var order = new List<ModuleNode>();
            var path = new List<string>();

            async Task VisitAsync(string descriptorPath, string? expectedId) {
                cancellationToken.ThrowIfCancellationRequested();
                descriptorPath = Path.GetFullPath(descriptorPath);
                if (!File.Exists(descriptorPath)) throw new FileNotFoundException($"Missing module configuration '{descriptorPath}'.", descriptorPath);
                var descriptorDirectory = Path.GetDirectoryName(descriptorPath)!;
                if (File.Exists(Path.Combine(descriptorDirectory, "module.zip"))) {
                    throw new NotSupportedException($"Immutable package dependency '{descriptorPath}' cannot be used as local source; remote package reuse is not supported.");
                }
                if (File.Exists(Path.Combine(descriptorDirectory, "module.json")) && File.Exists(Path.Combine(descriptorDirectory, "module.jsonc"))) {
                    throw new InvalidOperationException($"Source '{descriptorDirectory}' contains both module.json and module.jsonc.");
                }
                if (!byPath.TryGetValue(descriptorPath, out var node)) {
                    var root = await ReadJsonObjectAsync(descriptorPath, cancellationToken);
                    var modules = root["modules"] as JsonObject ?? throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain a modules object.");
                    var locals = modules.Where(item => item.Value is JsonObject value && !value.ContainsKey("configUrl")).ToArray();
                    if (locals.Length != 1) throw new InvalidOperationException($"Module configuration '{descriptorPath}' must contain exactly one local module definition.");
                    foreach (var item in modules) ValidateModuleId(item.Key);
                    var id = locals[0].Key;
                    var local = (JsonObject)locals[0].Value!;
                    var version = local["version"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException($"Module '{id}' must declare a non-empty version.");
                    ValidatePackageNamePart(version, "Module version");
                    var references = new List<Reference>();
                    foreach (var item in modules.OrderBy(item => item.Key, StringComparer.Ordinal)) {
                        if (item.Key == id) continue;
                        var reference = item.Value as JsonObject ?? throw new InvalidOperationException($"Module reference '{item.Key}' in '{descriptorPath}' must be an object.");
                        if (reference.ContainsKey("assetsUrl")) throw new InvalidOperationException($"Module reference '{item.Key}' cannot override assetsUrl.");
                        var configUrl = reference["configUrl"]?.GetValue<string>();
                        if (string.IsNullOrWhiteSpace(configUrl)) throw new InvalidOperationException($"Module reference '{item.Key}' in '{descriptorPath}' must declare configUrl.");
                        references.Add(new Reference(item.Key, ResolveConfigUrl(descriptorPath, configUrl)));
                    }
                    node = new ModuleNode(id, version, descriptorPath, root, references);
                    byPath.Add(descriptorPath, node);
                    if (byId.TryGetValue(id, out var existing) && !PathComparer().Equals(existing.DescriptorPath, descriptorPath)) {
                        throw new InvalidOperationException($"Module '{id}' is referenced with conflicting configUrl values: '{existing.DescriptorPath}' and '{descriptorPath}'.");
                    }
                    byId[id] = node;
                }
                if (expectedId != null && expectedId != node.Id) {
                    throw new InvalidOperationException($"Module reference '{expectedId}' points to '{descriptorPath}', but that configuration defines local module '{node.Id}'.");
                }
                if (states.TryGetValue(descriptorPath, out var state)) {
                    if (state == 2) return;
                    var cycle = string.Join(" -> ", path.Skip(path.IndexOf(node.Id)).Append(node.Id));
                    throw new InvalidOperationException($"Module dependency cycle detected: {cycle}.");
                }
                states[descriptorPath] = 1;
                path.Add(node.Id);
                foreach (var reference in node.References) await VisitAsync(reference.DescriptorPath, reference.Id);
                path.RemoveAt(path.Count - 1);
                states[descriptorPath] = 2;
                order.Add(node);
            }

            await VisitAsync(rootDescriptor, null);
            return order;
        }
        private static string ResolveConfigUrl(string ownerDescriptor, string value) {
            // only filesystem source documents can be compiled reliably at build time
            var path = value.StartsWith("url:", StringComparison.OrdinalIgnoreCase) ? value[4..] : value;
            if (path.Contains('?') || path.Contains('#')) throw new NotSupportedException($"Package dependency URL '{value}' cannot contain a query or fragment.");
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)) return Path.GetFullPath(uri.LocalPath);
            if (Uri.TryCreate(path, UriKind.Absolute, out uri) && !Path.IsPathFullyQualified(path)) {
                throw new NotSupportedException($"Remote module dependency '{value}' is not supported by pack. Provide a local source definition.");
            }
            if (path.StartsWith("app:", StringComparison.OrdinalIgnoreCase) || path.StartsWith('/')) {
                throw new NotSupportedException($"Application-root dependency URL '{value}' cannot be resolved without a deployment base.");
            }
            return Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(Path.GetDirectoryName(ownerDescriptor)!, path));
        }
        private static async Task<BuiltPackage> BuildModuleAsync(ModuleNode node, Dictionary<string, BuiltPackage> dependencies, string stagingPath, string outputPath,
            CancellationToken cancellationToken) {
            // retain the owner's local definition and reference contributions while changing only dependency URLs
            var workPath = Path.Combine(stagingPath, "work", node.Id);
            CopyDirectory(Path.GetDirectoryName(node.DescriptorPath)!, workPath, cancellationToken);
            var stagedDescriptor = Path.Combine(workPath, "module.json");
            var root = (JsonObject)Canonicalize(node.Configuration)!;
            var modules = (JsonObject)root["modules"]!;
            foreach (var reference in node.References) {
                var dependency = dependencies[reference.Id];
                ((JsonObject)modules[reference.Id]!)["configUrl"] = $"url:../../{dependency.RelativeDirectory.Replace('\\', '/')}/module.json";
            }
            foreach (var item in modules) (item.Value as JsonObject)?.Remove("builtAt");
            ((JsonObject)modules[node.Id]!)["hash"] = "";
            await File.WriteAllTextAsync(stagedDescriptor, root.ToJsonString(JsonOptions), cancellationToken);
            var jsoncPath = Path.Combine(workPath, "module.jsonc");
            if (File.Exists(jsoncPath)) File.Delete(jsoncPath);
            var indexPath = Path.Combine(workPath, FilesIndexer.ModuleFilesJson);
            if (File.Exists(indexPath)) File.Delete(indexPath);
            await CompileResourcesAsync(workPath, stagedDescriptor, cancellationToken);
            var inventory = await new FilesIndexer().CreateJsonAsync(workPath, cancellationToken);
            await File.WriteAllTextAsync(indexPath, inventory, cancellationToken);
            var hash = await ComputeHashAsync(workPath, cancellationToken);
            var relativeDirectory = $"{node.Id}/{node.Version}.{hash}";
            var packagePath = Path.Combine(stagingPath, "packages", node.Id, $"{node.Version}.{hash}");
            Directory.CreateDirectory(packagePath);
            if (!Directory.Exists(Path.Combine(outputPath, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)))) {
                var zipPath = Path.Combine(packagePath, "module.zip");
                await CreateZipAsync(workPath, zipPath, cancellationToken);
                if (await ComputeZipHashAsync(zipPath, cancellationToken) != hash) throw new InvalidOperationException($"Generated ZIP for '{node.Id}' does not match its package hash.");
            }
            ((JsonObject)modules[node.Id]!)["hash"] = hash;
            ((JsonObject)modules[node.Id]!)["assetsUrl"] = "url:./module.zip";
            ((JsonObject)modules[node.Id]!)["files"] = JsonNode.Parse(inventory);
            ((JsonObject)modules[node.Id]!)["builtAt"] = UtcTimestamp();
            await File.WriteAllTextAsync(Path.Combine(packagePath, "module.json"), root.ToJsonString(JsonOptions), cancellationToken);
            return new BuiltPackage(node.Id, node.Version, hash, relativeDirectory, "module.json");
        }
        private static async Task<BuiltPackage> BuildFrameworkAsync(string descriptorPath, string stagingPath, string outputPath, CancellationToken cancellationToken) {
            // retain the framework's separate descriptor and resource compilation rules
            var root = (JsonObject)Canonicalize(await ReadJsonObjectAsync(descriptorPath, cancellationToken))!;
            var framework = root["xshell"] as JsonObject ?? throw new InvalidOperationException("XShell configuration must contain xshell.");
            var version = framework["version"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(version)) throw new InvalidOperationException("XShell configuration must contain xshell.version.");
            ValidatePackageNamePart(version, "XShell version");
            framework.Remove("builtAt");
            var workPath = Path.Combine(stagingPath, "work", "xshell");
            CopyDirectory(Path.GetDirectoryName(descriptorPath)!, workPath, cancellationToken);
            foreach (var name in new[] { "bootstrap.js", "sw.js" }) {
                if (!File.Exists(Path.Combine(workPath, name))) throw new FileNotFoundException($"XShell framework source is missing required {name}.");
            }
            framework["hash"] = "";
            await File.WriteAllTextAsync(Path.Combine(workPath, "xshell.json"), root.ToJsonString(JsonOptions), cancellationToken);
            var jsonc = Path.Combine(workPath, "xshell.jsonc");
            if (File.Exists(jsonc)) File.Delete(jsonc);
            var indexPath = Path.Combine(workPath, FilesIndexer.ModuleFilesJson);
            if (File.Exists(indexPath)) File.Delete(indexPath);
            var inventory = await new FilesIndexer().CreateJsonAsync(workPath, cancellationToken);
            await File.WriteAllTextAsync(indexPath, inventory, cancellationToken);
            var hash = await ComputeHashAsync(workPath, cancellationToken);
            var relativeDirectory = $"xshell/{version}.{hash}";
            var packagePath = Path.Combine(stagingPath, "packages", "xshell", $"{version}.{hash}");
            Directory.CreateDirectory(packagePath);
            // expose exact canonical bootstrap files without changing the package hash or ZIP contents
            File.Copy(Path.Combine(workPath, "bootstrap.js"), Path.Combine(packagePath, "bootstrap.js"));
            File.Copy(Path.Combine(workPath, "sw.js"), Path.Combine(packagePath, "sw.js"));
            if (!Directory.Exists(Path.Combine(outputPath, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)))) {
                var zipPath = Path.Combine(packagePath, "xshell.zip");
                await CreateZipAsync(workPath, zipPath, cancellationToken);
                if (await ComputeZipHashAsync(zipPath, cancellationToken) != hash) throw new InvalidOperationException("Generated XShell ZIP does not match its package hash.");
            }
            framework["hash"] = hash;
            framework["assetsUrl"] = "url:./xshell.zip";
            framework["files"] = JsonNode.Parse(inventory);
            framework["builtAt"] = UtcTimestamp();
            await File.WriteAllTextAsync(Path.Combine(packagePath, "xshell.json"), root.ToJsonString(JsonOptions), cancellationToken);
            return new BuiltPackage("xshell", version, hash, relativeDirectory, "xshell.json");
        }
        private static async Task ValidateBuiltReferencesAsync(List<ModuleNode> order, Dictionary<string, BuiltPackage> packages, string stagingPath,
            CancellationToken cancellationToken) {
            // verify every published descriptor resolves to the staged canonical dependency identity
            foreach (var node in order) {
                var package = packages[node.Id];
                var descriptor = Path.Combine(stagingPath, "packages", package.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar), "module.json");
                var root = await ReadJsonObjectAsync(descriptor, cancellationToken);
                var modules = (JsonObject)root["modules"]!;
                foreach (var reference in node.References) {
                    var url = ((JsonObject)modules[reference.Id]!)["configUrl"]!.GetValue<string>();
                    var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(descriptor)!, url[4..]));
                    var expected = Path.GetFullPath(Path.Combine(stagingPath, "packages", packages[reference.Id].RelativeDirectory.Replace('/', Path.DirectorySeparatorChar), "module.json"));
                    if (!PathComparer().Equals(resolved, expected) || !File.Exists(resolved)) {
                        throw new InvalidOperationException($"Packaged reference '{node.Id}' -> '{reference.Id}' does not resolve to its staged descriptor.");
                    }
                }
            }
        }
        private static async Task PublishAsync(string outputPath, string stagingPath, BuiltPackage root, BuiltPackage framework, BuiltPackage[] packages,
            CancellationToken cancellationToken) {
            // the release manifest is the only visibility boundary; published packages are immutable
            Directory.CreateDirectory(outputPath);
            var lockPath = Path.Combine(outputPath, ".pack.lock");
            using var publicationLock = AcquirePublicationLock(lockPath);
            try {
                foreach (var package in packages) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var source = Path.Combine(stagingPath, "packages", package.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                    var target = Path.Combine(outputPath, package.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                    if (Directory.Exists(target)) {
                        await ValidateExistingPackageAsync(target, source, package, cancellationToken);
                        continue;
                    }
                    var zipName = package.Id == "xshell" ? "xshell.zip" : "module.zip";
                    if (!File.Exists(Path.Combine(source, zipName))) {
                        throw new IOException($"Previously published package '{target}' disappeared before commit; no staged ZIP is available.");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    Directory.Move(source, target);
                }

                var manifest = new JsonObject {
                    ["root"] = $"{root.RelativeDirectory}/{root.DescriptorName}",
                    ["xshell"] = $"{framework.RelativeDirectory}/{framework.DescriptorName}",
                    ["createdAt"] = UtcTimestamp(),
                    ["packages"] = new JsonArray(packages.Select(package => (JsonNode?)JsonValue.Create($"{package.RelativeDirectory}/{package.DescriptorName}")).ToArray())
                };
                var temporaryManifest = Path.Combine(outputPath, $".release-{Guid.NewGuid():N}.tmp");
                try {
                    await File.WriteAllTextAsync(temporaryManifest, manifest.ToJsonString(JsonOptions), cancellationToken);
                    var publishedManifest = Path.Combine(outputPath, ReleaseFileName);
                    if (File.Exists(publishedManifest)) File.Replace(temporaryManifest, publishedManifest, null);
                    else File.Move(temporaryManifest, publishedManifest);
                } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
                    throw new IOException($"Failed to commit XShell distribution in '{outputPath}'. The previous release remains selected.", exception);
                } finally {
                    if (File.Exists(temporaryManifest)) File.Delete(temporaryManifest);
                }
            } finally {
                // remove the name while the exclusive handle is held; a new writer can start only after all publication work is done
                if (File.Exists(lockPath)) File.Delete(lockPath);
            }
        }
        private static async Task ValidateExistingPackageAsync(string target, string source, BuiltPackage package, CancellationToken cancellationToken) {
            // compare canonical archive contents and all external metadata except the original package build time
            var descriptor = Path.Combine(target, package.DescriptorName);
            var archive = Path.Combine(target, package.Id == "xshell" ? "xshell.zip" : "module.zip");
            var isFramework = package.Id == "xshell";
            if (Directory.EnumerateFileSystemEntries(target).Count() != (isFramework ? 4 : 2) || !File.Exists(descriptor) || !File.Exists(archive) ||
                (isFramework && (!File.Exists(Path.Combine(target, "bootstrap.js")) || !File.Exists(Path.Combine(target, "sw.js"))))) {
                throw new InvalidOperationException($"Existing immutable package has an incomplete or unrecognized representation: {target}");
            }
            JsonObject existing;
            try {
                existing = await ReadJsonObjectAsync(descriptor, cancellationToken);
            } catch (JsonException exception) {
                throw new InvalidOperationException($"Existing immutable package identity collides with different or damaged content: {target}", exception);
            }
            var expected = await ReadJsonObjectAsync(Path.Combine(source, package.DescriptorName), cancellationToken);
            var existingLocal = package.Id == "xshell" ? existing["xshell"] as JsonObject : (existing["modules"] as JsonObject)?[package.Id] as JsonObject;
            if (existingLocal == null) throw new InvalidOperationException($"Existing immutable package has no local definition: {target}");
            if (existingLocal.TryGetPropertyValue("builtAt", out var builtAt) &&
                (builtAt is not JsonValue value || !value.TryGetValue<string>(out var timestamp) ||
                !DateTimeOffset.TryParseExact(timestamp, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out _))) {
                throw new InvalidOperationException($"Existing immutable package has an invalid builtAt timestamp: {target}");
            }
            existingLocal.Remove("builtAt");
            var expectedLocal = package.Id == "xshell" ? (JsonObject)expected["xshell"]! : (JsonObject)((JsonObject)expected["modules"]!)[package.Id]!;
            expectedLocal.Remove("builtAt");
            if (!JsonNode.DeepEquals(existing, expected) || await ComputeZipHashAsync(archive, cancellationToken) != package.Hash) {
                throw new InvalidOperationException($"Existing immutable package identity collides with different or damaged content: {target}");
            }
            if (isFramework) {
                foreach (var name in new[] { "bootstrap.js", "sw.js" }) {
                    var existingBytes = await File.ReadAllBytesAsync(Path.Combine(target, name), cancellationToken);
                    var expectedBytes = await File.ReadAllBytesAsync(Path.Combine(source, name), cancellationToken);
                    if (!existingBytes.AsSpan().SequenceEqual(expectedBytes)) {
                        throw new InvalidOperationException($"Existing immutable package identity collides with different or damaged content: {target}");
                    }
                }
            }
        }
        private static string UtcTimestamp() => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        private static FileStream AcquirePublicationLock(string lockPath) {
            try {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete);
            } catch (IOException exception) {
                throw new IOException($"Cannot acquire XShell distribution publication lock '{lockPath}'; another pack may be committing.", exception);
            }
        }
        private static async Task<string> ComputeZipHashAsync(string archivePath, CancellationToken cancellationToken) {
            using var archive = ZipFile.OpenRead(archivePath);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var entries = archive.Entries.Where(entry => !entry.FullName.EndsWith('/')).OrderBy(entry => entry.FullName, StringComparer.Ordinal).ToArray();
            if (entries.Select(entry => entry.FullName).Distinct(StringComparer.Ordinal).Count() != entries.Length) {
                throw new InvalidOperationException($"Archive '{archivePath}' contains duplicate entries.");
            }
            foreach (var entry in entries) {
                cancellationToken.ThrowIfCancellationRequested();
                var pathBytes = Encoding.UTF8.GetBytes(entry.FullName);
                var pathLength = new byte[4];
                var fileLength = new byte[8];
                BinaryPrimitives.WriteInt32LittleEndian(pathLength, pathBytes.Length);
                BinaryPrimitives.WriteInt64LittleEndian(fileLength, entry.Length);
                hash.AppendData(pathLength);
                hash.AppendData(pathBytes);
                hash.AppendData(fileLength);
                await using var stream = entry.Open();
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0) hash.AppendData(buffer.AsSpan(0, read));
            }
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()[..16];
        }
        private static async Task CreateZipAsync(string sourcePath, string archivePath, CancellationToken cancellationToken) {
            // sorted entries and a fixed ZIP timestamp make archive bytes reproducible
            await using var stream = File.Create(archivePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Select(full => (Full: full, Relative: Path.GetRelativePath(sourcePath, full).Replace('\\', '/')))
                .OrderBy(item => item.Relative, StringComparer.Ordinal)) {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = archive.CreateEntry(file.Relative, CompressionLevel.Optimal);
                entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                await using var input = File.OpenRead(file.Full);
                await using var output = entry.Open();
                await input.CopyToAsync(output, cancellationToken);
            }
        }
        private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken) {
            // hash canonical package paths and bytes, independent of ZIP metadata and staging location
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Select(full => (Full: full, Relative: Path.GetRelativePath(path, full).Replace('\\', '/')))
                .OrderBy(item => item.Relative, StringComparer.Ordinal)) {
                cancellationToken.ThrowIfCancellationRequested();
                var pathBytes = Encoding.UTF8.GetBytes(file.Relative);
                var pathLength = new byte[4];
                var fileLength = new byte[8];
                BinaryPrimitives.WriteInt32LittleEndian(pathLength, pathBytes.Length);
                hash.AppendData(pathLength);
                hash.AppendData(pathBytes);
                await using var stream = File.OpenRead(file.Full);
                BinaryPrimitives.WriteInt64LittleEndian(fileLength, stream.Length);
                hash.AppendData(fileLength);
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0) hash.AppendData(buffer.AsSpan(0, read));
            }
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()[..16];
        }
        private static async Task CompileResourcesAsync(string workPath, string descriptorPath, CancellationToken cancellationToken) {
            // compile authored resources once and reject conflicting HTML and JavaScript targets
            var resources = Directory.EnumerateFiles(workPath, "*", SearchOption.AllDirectories)
                .Where(file => new[] { ".js", ".html", ".css" }.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => Path.GetRelativePath(workPath, file), StringComparer.Ordinal).ToArray();
            var runtimePaths = new HashSet<string>(PathComparer());
            foreach (var resource in resources) {
                var target = Path.GetExtension(resource).Equals(".html", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(resource, ".js") : resource;
                if (!runtimePaths.Add(target)) throw new InvalidOperationException($"Conflicting module resources target '{Path.GetRelativePath(workPath, target)}'.");
            }
            var compiler = new ModuleFileCompiler();
            foreach (var resource in resources) {
                cancellationToken.ThrowIfCancellationRequested();
                var compiled = await compiler.CompileAsync(descriptorPath, resource, cancellationToken);
                if (Path.GetExtension(resource).Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                    await File.WriteAllTextAsync(Path.ChangeExtension(resource, ".js"), compiled.Content, cancellationToken);
                    File.Delete(resource);
                } else await File.WriteAllTextAsync(resource, compiled.Content, cancellationToken);
            }
        }
        private static async Task<JsonObject> ReadJsonObjectAsync(string path, CancellationToken cancellationToken) {
            return JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken), documentOptions: new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
            }) as JsonObject ?? throw new InvalidOperationException($"Invalid configuration '{path}'.");
        }
        private static JsonNode? Canonicalize(JsonNode? node) {
            // object key order is authored formatting, not part of package semantics
            if (node is JsonObject value) {
                var result = new JsonObject();
                foreach (var item in value.OrderBy(item => item.Key, StringComparer.Ordinal)) result[item.Key] = Canonicalize(item.Value);
                return result;
            }
            if (node is JsonArray array) return new JsonArray(array.Select(Canonicalize).ToArray());
            return node?.DeepClone();
        }
        private static string FindDescriptor(string directory, string name) {
            // reject ambiguous authored descriptors
            var json = Path.Combine(directory, name + ".json");
            var jsonc = Path.Combine(directory, name + ".jsonc");
            if (File.Exists(json) && File.Exists(jsonc)) throw new InvalidOperationException($"Source contains both {name}.json and {name}.jsonc.");
            return File.Exists(json) ? json : File.Exists(jsonc) ? jsonc :
                throw new FileNotFoundException($"Source must contain {name}.json or {name}.jsonc.");
        }
        private static string FindBundledFrameworkDescriptor() {
            // resolve shipped framework resources from the running assembly location
            var assemblyDirectory = Path.GetDirectoryName(typeof(Pack).Assembly.Location)!;
            var frameworkDirectory = Path.Combine(assemblyDirectory, "Resources", Extensions.ResourceName, "xshell");
            if (!Directory.Exists(frameworkDirectory)) throw new DirectoryNotFoundException($"Bundled XShell resources directory not found: {frameworkDirectory}");
            if (!File.Exists(Path.Combine(frameworkDirectory, "xshell.json")) && !File.Exists(Path.Combine(frameworkDirectory, "xshell.jsonc"))) {
                throw new FileNotFoundException($"Bundled XShell descriptor xshell.json or xshell.jsonc not found in '{frameworkDirectory}'.");
            }
            return FindDescriptor(frameworkDirectory, "xshell");
        }
        private static void ValidateModuleId(string id) {
            if (!Regex.IsMatch(id, ModuleIdPattern, RegexOptions.CultureInvariant) || id == "xshell") {
                throw new InvalidOperationException($"Invalid or reserved module id '{id}'.");
            }
        }
        private static void ValidatePackageNamePart(string value, string label) {
            if (value is "." or ".." || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar)) {
                throw new InvalidOperationException($"{label} '{value}' cannot be used in a package path.");
            }
        }
        private static StringComparer PathComparer() => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        private static bool IsSameOrInside(string path, string root) {
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            return PathComparer().Equals(normalizedPath, normalizedRoot) || normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        private static void CopyDirectory(string source, string destination, CancellationToken cancellationToken) {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.Ordinal)) {
                cancellationToken.ThrowIfCancellationRequested();
                var target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
        }
    }
}
