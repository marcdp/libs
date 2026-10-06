using System.Security.Cryptography;
using System.Text.Json;
using DProjects.XShell.Services;

namespace DProjects.XShell.Test {

    public sealed class FilesIndexerTests {

        // methods
        [Fact]
        public async Task CreateJsonAsync_IndexesNestedFilesWithNormalizedLogicalPaths() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("source");
            WriteFile(root, "root.txt", [1]);
            WriteFile(root, "folder/nested.txt", [2]);

            var files = ReadInventory(await new FilesIndexer().CreateJsonAsync(root, TestContext.Current.CancellationToken));

            Assert.Equal(["/folder/nested.txt", "/root.txt"], files.Select(file => file.Path));
            Assert.All(files, file => {
                Assert.StartsWith("/", file.Path);
                Assert.DoesNotContain('\\', file.Path);
            });
        }
        [Fact]
        public async Task CreateJsonAsync_ReportsExactByteSizeAndLowercaseSha256() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("source");
            byte[] contents = [0, 1, 127, 128, 255];
            WriteFile(root, "bytes.bin", contents);

            var file = Assert.Single(ReadInventory(await new FilesIndexer().CreateJsonAsync(root, TestContext.Current.CancellationToken)));

            Assert.Equal("/bytes.bin", file.Path);
            Assert.Equal(contents.LongLength, file.Size);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(contents)).ToLowerInvariant(), file.Hash);
            Assert.Matches("^[0-9a-f]{64}$", file.Hash);
        }
        [Fact]
        public async Task CreateJsonAsync_ExcludesInventoryFilesAtEveryDepthIgnoringCase() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("source");
            WriteFile(root, "module.files.json", [1]);
            WriteFile(root, "other/MODULE.FILES.JSON", [2]);
            WriteFile(root, "nested/module.files.json", [3]);
            WriteFile(root, "file.txt", [4]);
            WriteFile(root, "nested/keep.txt", [5]);

            var files = ReadInventory(await new FilesIndexer().CreateJsonAsync(root, TestContext.Current.CancellationToken));

            Assert.Equal(["/file.txt", "/nested/keep.txt"], files.Select(file => file.Path));
        }
        [Fact]
        public async Task CreateJsonAsync_SortsByOrdinalLogicalPathRegardlessOfCreationOrder() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("source");
            WriteFile(root, "z.txt", [1]);
            WriteFile(root, "folder/c.txt", [2]);
            WriteFile(root, "a.txt", [3]);
            WriteFile(root, "folder/b.txt", [4]);
            WriteFile(root, "B.txt", [5]);

            var files = ReadInventory(await new FilesIndexer().CreateJsonAsync(root, TestContext.Current.CancellationToken));

            Assert.Equal(["/B.txt", "/a.txt", "/folder/b.txt", "/folder/c.txt", "/z.txt"], files.Select(file => file.Path));
        }
        [Fact]
        public async Task CreateJsonAsync_IdenticalLogicalTreesProduceIdenticalJsonAcrossLocationsAndCreationOrders() {
            using var workspace = new TemporaryDirectory();
            var firstRoot = workspace.PathFor("first/source");
            var secondRoot = workspace.PathFor("second/source");
            var files = new (string Path, byte[] Contents)[] {
                ("z.txt", [0, 255]),
                ("folder/b.txt", [1, 2, 3]),
                ("a.txt", [4])
            };
            foreach (var file in files) WriteFile(firstRoot, file.Path, file.Contents);
            foreach (var file in files.Reverse()) WriteFile(secondRoot, file.Path, file.Contents);

            var indexer = new FilesIndexer();
            var firstJson = await indexer.CreateJsonAsync(firstRoot, TestContext.Current.CancellationToken);
            var secondJson = await indexer.CreateJsonAsync(secondRoot, TestContext.Current.CancellationToken);

            Assert.NotEqual(firstRoot, secondRoot);
            Assert.Equal(firstJson, secondJson);
        }
        [Fact]
        public async Task CreateJsonAsync_EmptyDirectoryProducesEmptyArray() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("empty");
            Directory.CreateDirectory(root);

            var files = ReadInventory(await new FilesIndexer().CreateJsonAsync(root, TestContext.Current.CancellationToken));

            Assert.Empty(files);
        }
        [Fact]
        public async Task CreateJsonAsync_AlreadyCancelledTokenThrows() {
            using var workspace = new TemporaryDirectory();
            var root = workspace.PathFor("source");
            WriteFile(root, "file.txt", [1]);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new FilesIndexer().CreateJsonAsync(root, cancellation.Token));
        }

        // methods (private)
        private static void WriteFile(string root, string relativePath, byte[] contents) {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, contents);
        }
        private static FileEntry[] ReadInventory(string json) {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.EnumerateArray().Select(item => new FileEntry(
                item.GetProperty("path").GetString()!,
                item.GetProperty("size").GetInt64(),
                item.GetProperty("hash").GetString()!
            )).ToArray();
        }

        // inner classes
        private sealed record FileEntry(string Path, long Size, string Hash);
        private sealed class TemporaryDirectory : IDisposable {

            // props
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Test", Guid.NewGuid().ToString("N"));

            // methods
            public string PathFor(string name) => Path.Combine(Root, name.Replace('/', Path.DirectorySeparatorChar));
            public void Dispose() {
                if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            }
        }
    }
}
