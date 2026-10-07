using System.IO.Compression;
using System.Security.Cryptography;
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
        public async Task ModuleExpanded_PublishesNormalizedCompiledTreeAndInventory() {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("expanded");

            var package = await PackAsync(source, output, zip: false);

            AssertPackagePath(package, output, ModuleId, ModuleVersion);
            Assert.False(File.Exists(Path.Combine(package, "module.jsonc")));
            Assert.False(File.Exists(Path.Combine(package, "page.html")));
            Assert.Contains("templateRenderer:", File.ReadAllText(Path.Combine(package, "page.js")));
            Assert.Equal("value", File.ReadAllText(Path.Combine(package, "data", "value.txt")));
            var publishedDescriptor = File.ReadAllText(Path.Combine(package, "module.json"));
            Assert.DoesNotContain("// authored module metadata", publishedDescriptor);
            using (var descriptor = JsonDocument.Parse(publishedDescriptor)) {
                var module = descriptor.RootElement.GetProperty("modules").GetProperty(ModuleId);
                Assert.Equal(ModuleVersion, module.GetProperty("version").GetString());
                Assert.Equal("Tiny module", module.GetProperty("label").GetString());
            }
            var files = ReadExpandedFiles(package);
            AssertInventoryMatchesFiles(File.ReadAllText(Path.Combine(package, "module.files.json")), files);
        }
        [Fact]
        public async Task ModuleZip_PublishesDescriptorAndCompiledArchiveWithEmbeddedInventory() {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("zip");

            var package = await PackAsync(source, output, zip: true);

            AssertPackagePath(package, output, ModuleId, ModuleVersion);
            Assert.Equal(["module.json", "module.zip"], Directory.GetFiles(package).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray());
            Assert.False(File.Exists(Path.Combine(package, "module.files.json")));
            var files = ReadZipFiles(Path.Combine(package, "module.zip"));
            Assert.Contains("module.json", files.Keys);
            Assert.DoesNotContain("module.jsonc", files.Keys);
            Assert.Contains("page.js", files.Keys);
            Assert.DoesNotContain("page.html", files.Keys);
            Assert.Contains("templateRenderer:", System.Text.Encoding.UTF8.GetString(files["page.js"]));
            Assert.Equal("value", System.Text.Encoding.UTF8.GetString(files["data/value.txt"]));
            Assert.Contains("module.files.json", files.Keys);
            using var descriptor = JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "module.json")));
            var module = descriptor.RootElement.GetProperty("modules").GetProperty(ModuleId);
            Assert.Equal("Tiny module", module.GetProperty("label").GetString());
            Assert.Equal("source:./module.zip", module.GetProperty("assetsUrl").GetString());
            var embedded = ReadInventory(module.GetProperty("files"));
            var archived = ReadInventory(System.Text.Encoding.UTF8.GetString(files["module.files.json"]));
            Assert.Equal(archived, embedded);
            AssertInventoryMatchesFiles(archived, files);
        }
        [Fact]
        public async Task XShellExpanded_PublishesDescriptorResourcesAndInventory() {
            using var workspace = new TemporaryDirectory();
            var source = CreateXShellSource(workspace.PathFor("source"));
            var output = workspace.PathFor("expanded");

            var package = await PackAsync(source, output, zip: false);

            AssertPackagePath(package, output, "xshell", XShellVersion);
            Assert.True(File.Exists(Path.Combine(package, "xshell.jsonc")));
            Assert.Equal("runtime", File.ReadAllText(Path.Combine(package, "assets", "runtime.txt")));
            AssertInventoryMatchesFiles(File.ReadAllText(Path.Combine(package, "module.files.json")), ReadExpandedFiles(package));
        }
        [Fact]
        public async Task XShellZip_PublishesDescriptorAndArchiveWithEmbeddedInventory() {
            using var workspace = new TemporaryDirectory();
            var source = CreateXShellSource(workspace.PathFor("source"));
            var output = workspace.PathFor("zip");

            var package = await PackAsync(source, output, zip: true);

            AssertPackagePath(package, output, "xshell", XShellVersion);
            Assert.Equal(["xshell.jsonc", "xshell.zip"], Directory.GetFiles(package).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray());
            Assert.False(File.Exists(Path.Combine(package, "module.files.json")));
            var files = ReadZipFiles(Path.Combine(package, "xshell.zip"));
            Assert.Equal("runtime", System.Text.Encoding.UTF8.GetString(files["assets/runtime.txt"]));
            Assert.Contains("module.files.json", files.Keys);
            using var descriptor = JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "xshell.jsonc")));
            var xshell = descriptor.RootElement.GetProperty("xshell");
            Assert.Equal("source:./xshell.zip", xshell.GetProperty("assetsUrl").GetString());
            var embedded = ReadInventory(xshell.GetProperty("files"));
            var archived = ReadInventory(System.Text.Encoding.UTF8.GetString(files["module.files.json"]));
            Assert.Equal(archived, embedded);
            AssertInventoryMatchesFiles(archived, files);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SameRepresentation_ReusesImmutableIdentity(bool zip) {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("output");

            var first = await PackAsync(source, output, zip);
            var second = await PackAsync(source, output, zip);

            Assert.Equal(first, second);
            Assert.Single(Directory.GetDirectories(Path.Combine(output, ModuleId)));
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DifferentRepresentation_RejectsImmutableIdentityCollision(bool firstZip) {
            using var workspace = new TemporaryDirectory();
            var source = CreateModuleSource(workspace.PathFor("source"));
            var output = workspace.PathFor("output");
            var first = await PackAsync(source, output, firstZip);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => PackAsync(source, output, !firstZip));

            Assert.Contains("already exists using another representation", exception.Message);
            Assert.Single(Directory.GetDirectories(Path.Combine(output, ModuleId)));
            Assert.True(Directory.Exists(first));
        }
        [Fact]
        public async Task Identity_DependsOnLogicalContentsAcrossCreationOrderAndRepresentation() {
            using var workspace = new TemporaryDirectory();
            var firstSource = CreateModuleSource(workspace.PathFor("source-a"));
            var secondSource = CreateModuleSource(workspace.PathFor("source-b"), reverseCreationOrder: true);

            var first = await PackAsync(firstSource, workspace.PathFor("expanded-a"), zip: false);
            var second = await PackAsync(secondSource, workspace.PathFor("expanded-b"), zip: false);
            var zipped = await PackAsync(firstSource, workspace.PathFor("zip"), zip: true);

            Assert.Equal(Path.GetFileName(first), Path.GetFileName(second));
            Assert.Equal(Path.GetFileName(first), Path.GetFileName(zipped));
        }

        // methods (private)
        private static string CreateModuleSource(string path, bool reverseCreationOrder = false) {
            Directory.CreateDirectory(path);
            var files = new (string Name, string Content)[] {
                ("module.jsonc", """
                    {
                        // authored module metadata
                        "modules": {
                            "sample": {
                                "version": "1.2.3",
                                "label": "Tiny module",
                                "defaults": { "page": { "renderEngine": "x" }, },
                            },
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
            File.WriteAllText(Path.Combine(path, "xshell.jsonc"), """
                { "xshell": { "version": "2.3.4" } }
                """);
            File.WriteAllText(Path.Combine(path, "assets", "runtime.txt"), "runtime");
            return path;
        }
        private static async Task<string> PackAsync(string source, string output, bool zip) {
            var command = new Pack(new TestEnvironment()) { Source = source, Output = output, Zip = zip };
            Assert.Equal(0, await command.ExecuteAsync(TestContext.Current.CancellationToken));
            var id = File.Exists(Path.Combine(source, "module.jsonc")) ? ModuleId : "xshell";
            return Assert.Single(Directory.GetDirectories(Path.Combine(output, id)));
        }
        private static void AssertPackagePath(string package, string output, string id, string version) {
            Assert.Equal(Path.Combine(output, id), Path.GetDirectoryName(package));
            var identity = Path.GetFileName(package);
            Assert.StartsWith(version + ".", identity);
            var hash = identity[(version.Length + 1)..];
            Assert.Matches("^[0-9a-f]{16}$", hash);
        }
        private static Dictionary<string, byte[]> ReadExpandedFiles(string package) {
            return Directory.GetFiles(package, "*", SearchOption.AllDirectories)
                .ToDictionary(file => Path.GetRelativePath(package, file).Replace('\\', '/'), File.ReadAllBytes, StringComparer.Ordinal);
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
        private static FileEntry[] ReadInventory(string json) {
            using var document = JsonDocument.Parse(json);
            return ReadInventory(document.RootElement);
        }
        private static FileEntry[] ReadInventory(JsonElement json) {
            return json.EnumerateArray().Select(item => new FileEntry(
                item.GetProperty("path").GetString()!,
                item.GetProperty("size").GetInt64(),
                item.GetProperty("hash").GetString()!
            )).ToArray();
        }
        private static void AssertInventoryMatchesFiles(string inventory, Dictionary<string, byte[]> files) {
            AssertInventoryMatchesFiles(ReadInventory(inventory), files);
        }
        private static void AssertInventoryMatchesFiles(FileEntry[] inventory, Dictionary<string, byte[]> files) {
            var expectedPaths = files.Keys.Where(path => path != "module.files.json").Order(StringComparer.Ordinal).Select(path => "/" + path).ToArray();
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
