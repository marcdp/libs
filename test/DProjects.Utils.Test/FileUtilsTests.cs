using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class FileUtilsTests {

        [Fact]
        public void WriteTextFileAtomic_ReplacesExistingFileWithoutBom() {
            // verifies atomic text writes replace existing content without changing the requested encoding
            var folder = Path.Combine(Path.GetTempPath(), $"dprojects-utils-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try {
                var path = Path.Combine(folder, "value.json");
                File.WriteAllText(path, "old");

                FileUtils.WriteTextFileAtomic(path, "new", new UTF8Encoding(false));

                Assert.Equal("new", File.ReadAllText(path));
                Assert.Equal(Encoding.UTF8.GetBytes("new"), File.ReadAllBytes(path));
                Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
            } finally {
                Directory.Delete(folder, true);
            }
        }
        [Fact]
        public async Task WriteTextFileAtomicAsync_CreateOnlyPreservesExistingFile() {
            // verifies create-only publication cannot overwrite an immutable destination
            var folder = Path.Combine(Path.GetTempPath(), $"dprojects-utils-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try {
                var path = Path.Combine(folder, "value.json");
                await File.WriteAllTextAsync(path, "original", TestContext.Current.CancellationToken);

                await Assert.ThrowsAsync<IOException>(() =>
                    FileUtils.WriteTextFileAtomicAsync(path, "replacement", new UTF8Encoding(false), false, TestContext.Current.CancellationToken));

                Assert.Equal("original", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
                Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
            } finally {
                Directory.Delete(folder, true);
            }
        }
        [Fact]
        public async Task WriteTextFileAtomicAsync_CancellationRemovesTemporaryFile() {
            // verifies cancellation leaves neither a destination nor an unpublished temporary file
            var folder = Path.Combine(Path.GetTempPath(), $"dprojects-utils-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try {
                var path = Path.Combine(folder, "value.json");
                using var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    FileUtils.WriteTextFileAtomicAsync(path, "value", new UTF8Encoding(false), cancellationToken: cancellationTokenSource.Token));

                Assert.False(File.Exists(path));
                Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
            } finally {
                Directory.Delete(folder, true);
            }
        }
    }
}
