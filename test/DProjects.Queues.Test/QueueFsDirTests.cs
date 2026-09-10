using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Queues;
using DProjects.Utils;

using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Queues.Tests {

    public class QueueFsDirTests {

        // methods
        [Fact]
        public async Task WriteRead_RoundTripsBodyHeadersAndGeneratedIdentity() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            var message = new Message("known body");
            message.Headers.Set("x-test", "retained");

            await queue.WriteAsync(message, TestContext.Current.CancellationToken);
            var result = await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal("known body", result.GetBodyAsString());
            Assert.Equal("retained", result.Headers.Get<string?>("x-test", null));
            Assert.True(Guid.TryParse(result.Headers.Get<string?>(Message.HEADER_X_ID, null), out _));
        }
        [Fact]
        public async Task EmptyQueue_ImmediateAndBoundedReadsReturnNull() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);

            Assert.Null(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await queue.ReadAsync(1, TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Read_ClaimsMessageSoItIsNotReturnedTwice() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            await queue.WriteAsync(new Message("once"), TestContext.Current.CancellationToken);

            Assert.NotNull(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Delete_RemovesTheMessageReturnedByRead() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            await queue.WriteAsync(new Message("delete me"), TestContext.Current.CancellationToken);
            var claimed = await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(claimed);
            await queue.DeleteAsync(claimed, TestContext.Current.CancellationToken);

            Assert.Empty(filesystem.GetEntries("/queue/cur", GetModes.Files));
            Assert.Null(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task MultipleMessages_AreEachClaimedExactlyOnce() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            var expected = new[] { "alpha", "beta", "gamma" };
            foreach (var body in expected) await queue.WriteAsync(new Message(body), TestContext.Current.CancellationToken);

            var actual = new List<string>();
            Message? message;
            while ((message = await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken)) != null) actual.Add(message.GetBodyAsString());

            Assert.Equal(expected.Order(), actual.Order());
            Assert.Equal(3, actual.Distinct().Count());
        }
        [Fact]
        public async Task ConcurrentReaders_CannotBothClaimTheSameMessage() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            await queue.WriteAsync(new Message("single"), TestContext.Current.CancellationToken);

            var results = await Task.WhenAll(
                Task.Run(() => queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken)),
                Task.Run(() => queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken)));

            Assert.Single(results, message => message != null);
        }
        [Fact]
        public async Task IndependentQueueInstances_CanUseSharedFilesystemStateSequentially() {
            using var filesystem = new FilesystemMem(false, false);
            using var firstQueue = CreateQueue(filesystem);
            using var secondQueue = CreateQueue(filesystem);
            await firstQueue.WriteAsync(new Message("shared filesystem message"), TestContext.Current.CancellationToken);

            var message = await secondQueue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(message);
            Assert.Equal("shared filesystem message", message.GetBodyAsString());
            Assert.Null(await firstQueue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Purge_ClearsOwnedStatesPreservesUnrelatedDataAndLeavesQueueUsable() {
            using var filesystem = new FilesystemMem(false, false);
            filesystem.CreateDirectory("/outside");
            filesystem.SaveTextFile("/outside/keep.txt", "keep", System.Text.Encoding.UTF8);
            using var queue = CreateQueue(filesystem);
            await queue.WriteAsync(new Message("pending"), TestContext.Current.CancellationToken);
            await queue.WriteAsync(new Message("claimed"), TestContext.Current.CancellationToken);
            Assert.NotNull(await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken));

            await queue.PurgeAsync(TestContext.Current.CancellationToken);

            Assert.Empty(filesystem.GetEntries("/queue/new", GetModes.Files));
            Assert.Empty(filesystem.GetEntries("/queue/cur", GetModes.Files));
            Assert.Equal("keep", filesystem.LoadTextFile("/outside/keep.txt", System.Text.Encoding.UTF8));
            await queue.WriteAsync(new Message("after purge"), TestContext.Current.CancellationToken);
            Assert.Equal("after purge", (await queue.ReadAsync(cancellationToken: TestContext.Current.CancellationToken))!.GetBodyAsString());
        }
        [Fact]
#pragma warning disable xUnit1051 // deliberately verifies a caller-provided canceled token
        public async Task Operations_ObservePreCanceledTokens() {
            using var filesystem = new FilesystemMem(false, false);
            using var queue = CreateQueue(filesystem);
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.WriteAsync(new Message("x"), source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.ReadAsync(1000, source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.PurgeAsync(source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queue.DeleteAsync(new Message("x"), source.Token));
        }
#pragma warning restore xUnit1051

        // methods (private)
        private static QueueFsDir CreateQueue(IFilesystem filesystem) {
            return new QueueFsDir(filesystem, "/queue", NullLogger<IFilesystem>.Instance);
        }
    }
}
