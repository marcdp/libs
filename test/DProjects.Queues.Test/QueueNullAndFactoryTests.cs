using DProjects.Factories;
using DProjects.Fs;
using DProjects.Queues;

using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Queues.Tests {

    public class QueueNullAndFactoryTests {

        // methods
        [Fact]
        public async Task QueueNull_DiscardsWritesAndHasHarmlessLifecycleOperations() {
            using var queue = new QueueNull();

            await queue.WriteAsync(new Message("discarded"), TestContext.Current.CancellationToken);
            Assert.Null(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
            await queue.DeleteAsync(new Message("ignored"), TestContext.Current.CancellationToken);
            await queue.PurgeAsync(TestContext.Current.CancellationToken);
        }
        [Fact]
        public void Factories_CreateExpectedImplementationsAndForwardConfiguration() {
            using var filesystem = new FilesystemMem(false, false);
            var fsDir = new QueueFsDirFactory(filesystem, NullLogger<IFilesystem>.Instance);
            var recordingFactory = new RecordingFilesystemFactory(filesystem);
            var file = new QueueFileFactory(recordingFactory, NullLogger<IFilesystem>.Instance);

            Assert.IsType<QueueFsDir>(fsDir.Create("fs-dir:/queue-path"));
            Assert.IsType<QueueNull>(new QueueNullFactory().Create("null:"));
            Assert.IsType<QueueFsDir>(file.Create("file:/C:/queue-path?init=true"));
            Assert.EndsWith("?init=true", recordingFactory.Source, StringComparison.Ordinal);
        }

        private sealed class RecordingFilesystemFactory(IFilesystem filesystem) : IFactoryByUrl<IFilesystem> {

            // props
            public string Source { get; private set; } = "";

            // methods
            public IFilesystem Create(string src) {
                Source = src;
                return filesystem;
            }
        }
    }
}
