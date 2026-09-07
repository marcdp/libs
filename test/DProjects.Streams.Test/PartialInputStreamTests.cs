using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests {


    public class PartialInputStreamTests {


        [Theory]
        [InlineData(2, 3, "llo")]
        [InlineData(11, 5, "")]
        [InlineData(20, 5, "")]
        [InlineData(2, 4, "llo ")]
        [InlineData(2, 20, "llo world")]
        [InlineData(2, 0, "")]
        [InlineData(6, -1, "world")]
        public void Read_ExposesRequestedSeekableRange(long offset, long length, string expected) {
            using var source = CreateSource();
            using var partial = new PartialInputStream(source, offset, length);

            Assert.Equal(expected, ReadToEnd(partial));
            Assert.Equal(expected.Length, partial.BytesRead);
            Assert.Equal(expected.Length, partial.Position);
        }

        [Theory]
        [InlineData(2, 3, "llo")]
        [InlineData(20, 5, "")]
        [InlineData(6, -1, "world")]
        public void Read_ExposesRequestedNonSeekableRange(long offset, long length, string expected) {
            using var source = new NonSeekableStream(CreateSource());
            using var partial = new PartialInputStream(source, offset, length);

            Assert.Equal(expected, ReadToEnd(partial));
            Assert.Equal(expected.Length, partial.BytesRead);
        }

        [Fact]
        public void Read_MultipleSmallReadsNeverCrossBoundary() {
            using var source = CreateSource();
            using var partial = new PartialInputStream(source, 1, 5);
            var buffer = new byte[2];

            Assert.Equal(2, partial.Read(buffer, 0, buffer.Length));
            Assert.Equal("el", Encoding.UTF8.GetString(buffer));
            Assert.Equal(3, partial.BytesLeft);
            Assert.Equal(2, partial.Read(buffer, 0, buffer.Length));
            Assert.Equal("lo", Encoding.UTF8.GetString(buffer));
            Assert.Equal(1, partial.Read(buffer, 0, buffer.Length));
            Assert.Equal((byte)' ', buffer[0]);
            Assert.Equal(0, partial.BytesLeft);
            Assert.Equal(0, partial.Read(buffer, 0, buffer.Length));
            Assert.Equal(5, partial.BytesRead);
        }

        [Theory]
        [InlineData(false, 2, 4, "llo ")]
        [InlineData(false, 20, 4, "")]
        [InlineData(true, 2, 4, "llo ")]
        [InlineData(true, 20, 4, "")]
        [InlineData(true, 6, -1, "world")]
        public async Task ReadAsync_MatchesSyncBoundaryBehavior(bool nonSeekable, long offset, long length, string expected) {
            using var inner = CreateSource();
            using Stream source = nonSeekable ? new NonSeekableStream(inner, true) : inner;
            using var partial = new PartialInputStream(source, offset, length, true);
            var buffer = new byte[2];
            using var output = new MemoryStream();

            int read;
            while ((read = await partial.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken)) != 0) {
                await output.WriteAsync(buffer.AsMemory(0, read), TestContext.Current.CancellationToken);
            }

            Assert.Equal(expected, Encoding.UTF8.GetString(output.ToArray()));
            Assert.Equal(expected.Length, partial.BytesRead);
        }

        [Fact]
        public async Task ReadAsync_CancellationDuringSkipIsPropagatedAndSkipCanResume() {
            using var inner = CreateSource();
            using var cancellationSource = new CancellationTokenSource();
            using var source = new CancelAfterFirstAsyncReadStream(inner, cancellationSource);
            using var partial = new PartialInputStream(source, 6, -1);
            var buffer = new byte[16];

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => partial.ReadAsync(buffer, 0, buffer.Length, cancellationSource.Token));

            Assert.Equal(5, partial.Read(buffer, 0, buffer.Length));
            Assert.Equal("world", Encoding.UTF8.GetString(buffer, 0, 5));
        }

        [Fact]
        public async Task ReadAsync_PreCanceledReadDoesNotConsumeSource() {
            using var source = CreateSource();
            using var partial = new PartialInputStream(source, 2, -1);
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => partial.ReadAsync(new byte[1], 0, 1, cancellationSource.Token));

            Assert.Equal(0, source.Position);
            Assert.Equal(0, partial.BytesRead);
        }

        [Fact]
        public void Length_IsRelativeToPartialViewForSeekableSource() {
            using var source = CreateSource();

            using var bounded = new PartialInputStream(source, 2, 20, true);
            Assert.Equal(9, bounded.Length);

            using var unbounded = new PartialInputStream(source, 6, -1, true);
            Assert.Equal(5, unbounded.Length);

            using var beyondEnd = new PartialInputStream(source, 20, 5, true);
            Assert.Equal(0, beyondEnd.Length);
        }

        [Fact]
        public void Length_IsUnsupportedForNonSeekableSource() {
            using var source = new NonSeekableStream(CreateSource());
            using var partial = new PartialInputStream(source, 0, 5);

            Assert.Throws<NotSupportedException>(() => partial.Length);
        }

        [Fact]
        public void BytesLeft_UnboundedStreamIsUnsupported() {
            using var partial = new PartialInputStream(CreateSource(), 0, -1);

            Assert.Throws<NotSupportedException>(() => partial.BytesLeft);
        }

        [Fact]
        public void Capabilities_AreReadOnlyAndNonSeekable() {
            using var partial = new PartialInputStream(CreateSource(), 0, 5);

            Assert.True(partial.CanRead);
            Assert.False(partial.CanSeek);
            Assert.False(partial.CanWrite);
            Assert.Throws<NotSupportedException>(() => partial.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => partial.Position = 0);
            Assert.Throws<NotSupportedException>(() => partial.SetLength(1));
            Assert.Throws<NotSupportedException>(() => partial.Write(new byte[1], 0, 1));
            Assert.Throws<NotSupportedException>(() => {
                _ = partial.WriteAsync(new byte[1], 0, 1, CancellationToken.None);
            });
        }

        [Fact]
        public void Constructor_RejectsInvalidArguments() {
            Assert.Throws<ArgumentNullException>(() => new PartialInputStream(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PartialInputStream(Stream.Null, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PartialInputStream(Stream.Null, 0, -2));
        }

        [Fact]
        public void Read_ValidatesArgumentsEvenAfterBoundaryIsConsumed() {
            using var partial = new PartialInputStream(CreateSource(), 0, 0);

            Assert.Throws<ArgumentNullException>(() => partial.Read(null!, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => partial.Read(new byte[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => partial.Read(new byte[1], 0, -1));
            Assert.Throws<ArgumentException>(() => partial.Read(new byte[1], 1, 1));
        }

        [Fact]
        public void Dispose_RespectsLeaveOpen() {
            var leftOpen = new TrackingMemoryStream();
            new PartialInputStream(leftOpen, 0, 1, true).Dispose();
            Assert.False(leftOpen.IsDisposed);
            leftOpen.Dispose();

            var owned = new TrackingMemoryStream();
            var owner = new PartialInputStream(owned, 0, 1);
            owner.Dispose();
            owner.Dispose();
            Assert.True(owned.IsDisposed);
            Assert.Equal(1, owned.DisposeCount);
        }


        private static MemoryStream CreateSource() {
            return new MemoryStream(Encoding.UTF8.GetBytes("Hello world"));
        }
        private static string ReadToEnd(Stream stream) {
            var buffer = new byte[3];
            using var output = new MemoryStream();
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) != 0) {
                output.Write(buffer, 0, read);
            }
            return Encoding.UTF8.GetString(output.ToArray());
        }


        private class NonSeekableStream : Stream {

            private readonly Stream mInner;
            private readonly bool mLeaveOpen;

            public NonSeekableStream(Stream inner, bool leaveOpen = false) {
                mInner = inner;
                mLeaveOpen = leaveOpen;
            }

            public override bool CanRead => mInner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() {
            }
            public override int Read(byte[] buffer, int offset, int count) {
                return mInner.Read(buffer, offset, count);
            }
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
                return mInner.ReadAsync(buffer, offset, count, cancellationToken);
            }
            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotSupportedException();
            }
            public override void SetLength(long value) {
                throw new NotSupportedException();
            }
            public override void Write(byte[] buffer, int offset, int count) {
                throw new NotSupportedException();
            }
            protected override void Dispose(bool disposing) {
                if (disposing && !mLeaveOpen) mInner.Dispose();
                base.Dispose(disposing);
            }

        }


        private sealed class CancelAfterFirstAsyncReadStream : NonSeekableStream {

            private readonly CancellationTokenSource mCancellationSource;
            private bool mCanceled;

            public CancelAfterFirstAsyncReadStream(Stream inner, CancellationTokenSource cancellationSource)
                : base(inner) {
                mCancellationSource = cancellationSource;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
                if (!mCanceled) {
                    mCanceled = true;
                    int read = await base.ReadAsync(buffer, offset, 2, cancellationToken);
                    mCancellationSource.Cancel();
                    return read;
                }
                return await base.ReadAsync(buffer, offset, count, cancellationToken);
            }

        }


        private sealed class TrackingMemoryStream : MemoryStream {

            public bool IsDisposed { get; private set; }
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                DisposeCount++;
                base.Dispose(disposing);
            }

        }

    }

}
