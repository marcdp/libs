using System.Text;

using DProjects.Factories;
using DProjects.Fs;
using DProjects.Utils;

using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Cache.Tests {

    public class BlobCacheFactoryTests {

        // methods
        [Fact]
        public void NullFactory_CreatesNullCache() {
            var factory = new BlobCacheNullFactory();

            using var cache = factory.Create("null:");

            Assert.IsType<BlobCacheNull>(cache);
        }
        [Fact]
        public void FsDirFactory_UsesAbsoluteUrlPath() {
            using var filesystem = new FilesystemMem(false, false);
            filesystem.CreateDirectory("/cache");
            var factory = new BlobCacheFsDirFactory(filesystem, NullLogger<IFilesystem>.Instance);
            using var cache = factory.Create("fs-dir:///cache");
            using var source = new BlobCacheEntry("factory-key", new MemoryStream(Encoding.UTF8.GetBytes("payload")));

            cache.Set(source);

            Assert.Single(filesystem.GetEntries("/cache", GetModes.Files));
            using var result = cache.Get("factory-key");
            Assert.NotNull(result);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FileFactory_ForwardsPlatformPathAndInitQuery(bool init) {
            using var filesystem = new FilesystemMem(false, false);
            var filesystems = new CapturingFilesystemFactory(filesystem);
            var factory = new BlobCacheFileFactory(filesystems, NullLogger<IFilesystem>.Instance);
            var source = EnvironmentUtils.IsWindows() ? "file:///C:/cache" : "file:///var/cache";
            if (init) source += "?init=true";

            using var cache = factory.Create(source);

            var expectedPath = EnvironmentUtils.IsWindows() ? "C:" + Path.DirectorySeparatorChar + "cache" : "/var/cache";
            Assert.Equal(expectedPath + (init ? "?init=true" : ""), filesystems.LastSource);
            Assert.IsType<BlobCacheFsDir>(cache);
        }

        private sealed class CapturingFilesystemFactory(IFilesystem filesystem) : IFactoryByUrl<IFilesystem> {

            // props
            public string? LastSource { get; private set; }

            // methods
            public IFilesystem Create(string src) {
                LastSource = src;
                return filesystem;
            }
        }
    }
}