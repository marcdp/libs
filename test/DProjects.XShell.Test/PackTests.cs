using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using DProjects.Commands;
using DProjects.XShell.Commands;

namespace DProjects.XShell.Test {

    public sealed class PackTests {

        // consts
        private const string ModuleId = "sample";
        private const string ModuleVersion = "1.2.3";
        private const string XShellVersion = "2.3.4";

        // methods
        [Fact]
        public async Task Module_PublishesImmutableZipAndInventory() {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("dist");

            var package = await PackAsync(source, output);

            AssertPackagePath(package, output, ModuleId, ModuleVersion);
            Assert.Equal(["index.html", "module.json", "module.zip", "sw.js"], Directory.GetFiles(package).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray());
            Assert.True(File.Exists(Path.Combine(output, "release.json")));
            Assert.False(File.Exists(Path.Combine(source, "module.json")));
            Assert.True(File.Exists(Path.Combine(source, "module.jsonc")));
            Assert.True(File.Exists(Path.Combine(source, "page.html")));
            var files = ReadZipFiles(Path.Combine(package, "module.zip"));
            Assert.Contains("module.json", files.Keys);
            Assert.DoesNotContain("module.jsonc", files.Keys);
            Assert.Contains("page.js", files.Keys);
            Assert.DoesNotContain("page.html", files.Keys);
            Assert.Contains("templateRenderer:", Encoding.UTF8.GetString(files["page.js"]));
            Assert.Equal("value", Encoding.UTF8.GetString(files["data/value.txt"]));
            using var descriptor = JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "module.json")));
            var module = descriptor.RootElement.GetProperty("modules").GetProperty(ModuleId);
            Assert.Equal(ModuleVersion, module.GetProperty("version").GetString());
            Assert.Equal("Tiny module", module.GetProperty("label").GetString());
            Assert.Equal("url:./module.zip", module.GetProperty("assets").GetProperty("url").GetString());
            AssertInventoryMatchesFiles(ReadInventory(module.GetProperty("files")), files, "index.html", "sw.js");
        }
        [Fact]
        public async Task Pack_RecursivelyPublishesDependenciesAndRewritesReferences() {
            using var workspace = new TemporaryDirectory();
            var sourceRoot = workspace.PathFor("sources");
            var appSource = Path.Combine(sourceRoot, "app");
            var middleSource = Path.Combine(sourceRoot, "middle");
            var leafSource = Path.Combine(sourceRoot, "leaf");
            CreateModuleSource(leafSource, moduleId: "leaf");
            CreateModuleSource(middleSource, moduleId: "middle", references: """
                "leaf": { "configUrl": "url:../leaf/module.jsonc" }
                """);
            CreateModuleSource(appSource, references: """
                "middle": { "configUrl": "url:../middle/module.jsonc" }
                """);
            var output = workspace.PathFor("dist");

            var rootPackage = await PackAsync(appSource, output);

            AssertPackagePath(rootPackage, output, ModuleId, ModuleVersion);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "release.json")));
            Assert.Equal(Path.GetRelativePath(output, Path.Combine(rootPackage, "module.json")).Replace('\\', '/'),
                manifest.RootElement.GetProperty("root").GetString());
            Assert.Equal(4, manifest.RootElement.GetProperty("packages").GetArrayLength());
            var app = ReadModuleDescriptor(rootPackage);
            var middleReference = app.GetProperty("modules").GetProperty("middle");
            Assert.StartsWith("url:../../middle/", middleReference.GetProperty("configUrl").GetString());
            var middleDescriptorPath = Path.GetFullPath(Path.Combine(rootPackage, middleReference.GetProperty("configUrl").GetString()![4..]));
            Assert.True(File.Exists(middleDescriptorPath));
            using var middleDocument = JsonDocument.Parse(File.ReadAllText(middleDescriptorPath));
            var leafReference = middleDocument.RootElement.GetProperty("modules").GetProperty("leaf");
            Assert.StartsWith("url:../../leaf/", leafReference.GetProperty("configUrl").GetString());
            Assert.True(File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(middleDescriptorPath)!, leafReference.GetProperty("configUrl").GetString()![4..]))));
            Assert.Single(Directory.GetDirectories(Path.Combine(output, "leaf")));
        }
        [Fact]
        public async Task XShell_PublishesItsDistinctImmutableZipPackage() {
            using var workspace = new TemporaryDirectory();
            var source = CreateXShellSource(workspace.PathFor("source"));
            var output = workspace.PathFor("dist");

            var package = await PackAsync(source, output);

            AssertPackagePath(package, output, "xshell", XShellVersion);
            Assert.Equal(["bootstrap.js", "sw.js", "xshell.json", "xshell.zip"], Directory.GetFiles(package).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray());
            var files = ReadZipFiles(Path.Combine(package, "xshell.zip"));
            Assert.Equal("runtime", Encoding.UTF8.GetString(files["assets/runtime.txt"]));
            using var descriptor = JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "xshell.json")));
            var xshell = descriptor.RootElement.GetProperty("xshell");
            Assert.Equal("url:./xshell.zip", xshell.GetProperty("assets").GetProperty("url").GetString());
            AssertInventoryMatchesFiles(ReadInventory(xshell.GetProperty("files")), files);
        }
        [Fact]
        public async Task RepeatedPack_ReusesImmutableIdentityAndRemovesLockFile() {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("dist");

            var first = await PackAsync(source, output);
            var second = await PackAsync(source, output);

            Assert.Equal(first, second);
            Assert.Single(Directory.GetDirectories(Path.Combine(output, ModuleId)));
            Assert.False(File.Exists(Path.Combine(output, ".pack.lock")));
        }
        [Fact]
        public async Task Identity_IsStableAcrossFileCreationOrderAndOutputDirectories() {
            using var workspace = new TemporaryDirectory();
            var firstSource = CreateModuleSource(workspace.PathFor("source-a"));
            var secondSource = CreateModuleSource(workspace.PathFor("source-b"), reverseCreationOrder: true);

            var first = await PackAsync(firstSource, workspace.PathFor("dist-a"));
            var second = await PackAsync(secondSource, workspace.PathFor("dist-b"));

            Assert.Equal(Path.GetFileName(first), Path.GetFileName(second));
            Assert.Equal(File.ReadAllBytes(Path.Combine(first, "module.zip")), File.ReadAllBytes(Path.Combine(second, "module.zip")));
        }
        [Theory]
        [InlineData("")]
        [InlineData("XDemo")]
        [InlineData("123")]
        [InlineData("x_demo")]
        [InlineData("x.demo")]
        [InlineData("x/demo")]
        [InlineData("x demo")]
        [InlineData("-x-demo")]
        [InlineData("x-demo-")]
        [InlineData("x--demo")]
        [InlineData("café")]
        [InlineData("foo%20bar")]
        public async Task Module_PackRejectsMalformedModuleId(string moduleId) {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"), moduleId: moduleId);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => PackAsync(source, workspace.PathFor("dist"), moduleId: moduleId));

            Assert.Contains("Invalid or reserved module id", exception.Message);
        }

        // methods (private)
        private static string CreateModuleSource(string path, bool reverseCreationOrder = false, string moduleId = ModuleId, string references = "") {
            Directory.CreateDirectory(path);
            var referenceJson = string.IsNullOrWhiteSpace(references) ? "" : "," + references;
            var files = new (string Name, string Content)[] {
                ("module.jsonc", $$"""
                    {
                        // authored module metadata
                        "modules": {
                            "{{moduleId}}": {
                                "version": "{{ModuleVersion}}",
                                "label": "Tiny module",
                                "defaults": { "page": { "renderEngine": "x" }, },
                            }{{referenceJson}}
                        },
                    }
                    """),
                ("page.html", "<template><p>pack</p></template>"),
                ("data/value.txt", "value")
            };
            foreach (var file in reverseCreationOrder ? files.Reverse() : files) {
                var target = Path.Combine(path, file.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, file.Content);
            }
            return path;
        }
        private static string CreateXShellSource(string path) {
            Directory.CreateDirectory(Path.Combine(path, "assets"));
            File.WriteAllText(Path.Combine(path, "xshell.jsonc"), $$"""
                { "xshell": { "version": "{{XShellVersion}}" } }
                """);
            File.WriteAllText(Path.Combine(path, "assets", "runtime.txt"), "runtime");
            File.WriteAllText(Path.Combine(path, "bootstrap.js"), "bootstrap");
            File.WriteAllText(Path.Combine(path, "sw.js"), "worker");
            return path;
        }
        private static async Task<string> PackAsync(string source, string output, string moduleId = ModuleId) {
            var command = new Pack(new TestEnvironment()) { Source = source, Output = output };
            Assert.Equal(0, await command.ExecuteAsync(TestContext.Current.CancellationToken));
            var id = File.Exists(Path.Combine(source, "module.jsonc")) || File.Exists(Path.Combine(source, "module.json")) ? moduleId : "xshell";
            return Path.Combine(output, Assert.Single(Directory.GetDirectories(Path.Combine(output, id))));
        }
        private static JsonElement ReadModuleDescriptor(string package) {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "module.json")));
            return document.RootElement.Clone();
        }
        private static void AssertPackagePath(string package, string output, string id, string version) {
            Assert.Equal(Path.Combine(output, id), Path.GetDirectoryName(package));
            var identity = Path.GetFileName(package);
            Assert.StartsWith(version + ".", identity);
            var hash = identity[(version.Length + 1)..];
            Assert.Matches("^[0-9a-f]{16}$", hash);
        }
        private static Dictionary<string, byte[]> ReadZipFiles(string archivePath) {
            using var archive = ZipFile.OpenRead(archivePath);
            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var entry in archive.Entries.Where(entry => entry.Name.Length > 0)) {
                using var stream = entry.Open();
                using var bytes = new MemoryStream();
                stream.CopyTo(bytes);
                files.Add(entry.FullName, bytes.ToArray());
            }
            return files;
        }
        private static FileEntry[] ReadInventory(JsonElement json) {
            return json.EnumerateArray().Select(item => new FileEntry(
                item.GetProperty("path").GetString()!,
                item.GetProperty("size").GetInt64(),
                item.GetProperty("hash").GetString()!
            )).ToArray();
        }
        private static void AssertInventoryMatchesFiles(FileEntry[] inventory, Dictionary<string, byte[]> files, params string[] additionalExclusions) {
            var expectedPaths = files.Keys.Where(path => path is not ("module.json" or "xshell.json") && !additionalExclusions.Contains(path, StringComparer.Ordinal))
                .Order(StringComparer.Ordinal).Select(path => "/" + path).ToArray();
            Assert.Equal(expectedPaths, inventory.Select(item => item.Path));
            foreach (var item in inventory) {
                Assert.Matches("^/[A-Za-z0-9._/-]+$", item.Path);
                Assert.Matches("^[0-9a-f]{64}$", item.Hash);
                var contents = files[item.Path[1..]];
                Assert.Equal(contents.LongLength, item.Size);
                Assert.Equal(Convert.ToHexString(SHA256.HashData(contents)).ToLowerInvariant(), item.Hash);
            }
        }

        // inner classes
        private sealed record FileEntry(string Path, long Size, string Hash);
        private sealed class TemporaryDirectory : IDisposable {

            // props
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Test", Guid.NewGuid().ToString("N"));

            // methods
            public string PathFor(string name) => Path.Combine(Root, name);
            public void Dispose() {
                if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            }
        }
        private sealed class TestEnvironment : IEnvironment {

            // props
            public IInput In => throw new NotSupportedException();
            public IOutput Out { get; } = new DProjects.Commands.Environment.Output(DProjects.Commands.Environment.Output.Mode.Output);
            public IOutput Err => throw new NotSupportedException();

            // methods
            public void GetVariable(string name) => throw new NotSupportedException();
            public void SetVariable(string name, string value) => throw new NotSupportedException();
            public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
