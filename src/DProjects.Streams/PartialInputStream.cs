using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class PartialInputStream : Stream {


        //variables
        private readonly Stream mStream;

        private readonly long mOffset;
        private long mBytesToSkip;
        private bool mOffsetInitialized;
        private bool mSourceExhaustedWhileSkipping;
        private readonly long mMaxBytesToRead;
        private long mBytesRead;

        private readonly bool mLeaveOpen;
        private bool mDisposed;


        //constructor
        public PartialInputStream(Stream stream, long offset, long length, bool leaveOpen = false) {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < -1) throw new ArgumentOutOfRangeException(nameof(length));

            mStream = stream;
            mOffset = offset;
            mBytesToSkip = offset;
            mMaxBytesToRead = length;
            mLeaveOpen = leaveOpen;
        }
        protected override void Dispose(bool disposing) {
            if (disposing && !mDisposed) {
                mDisposed = true;
                if (!mLeaveOpen) mStream.Dispose();
            }
            base.Dispose(disposing);
        }


        //properties
        public override bool CanRead => !mDisposed && mStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Position {
            get {
                ThrowIfDisposed();
                return mBytesRead;
            }
            set { throw new NotSupportedException("This stream does not support seeking."); }
        }
        public override long Length {
            get {
                ThrowIfDisposed();
                if (!mStream.CanSeek) throw new NotSupportedException("The length of the partial stream cannot be determined.");

                long streamLength = mStream.Length;
                long availableLength = mOffset >= streamLength ? 0 : streamLength - mOffset;
                return mMaxBytesToRead == -1 ? availableLength : Math.Min(mMaxBytesToRead, availableLength);
            }
        }
        public long BytesLeft {
            get {
                if (mMaxBytesToRead == -1) throw new NotSupportedException("The number of bytes left in an unbounded stream cannot be determined.");
                return Math.Max(0, mMaxBytesToRead - mBytesRead);
            }
        }
        public long BytesRead => mBytesRead;


        //methods
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void Flush() {
            ThrowIfDisposed();
        }
        public override Task FlushAsync(CancellationToken cancellationToken) {
            ThrowIfDisposed();
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
            return Task.CompletedTask;
        }
        public override void SetLength(long value) {
            throw new NotSupportedException("This stream does not support writing.");
        }
        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("This stream does not support writing.");
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            throw new NotSupportedException("This stream does not support writing.");
        }
        public override int Read(byte[] buffer, int offset, int count) {
            ValidateReadArguments(buffer, offset, count);
            ThrowIfDisposed();
            InitializeOffset();

            int bytesToRead = GetBytesToRead(count);
            if (bytesToRead == 0) return 0;

            int bytesJustRead = mStream.Read(buffer, offset, bytesToRead);
            mBytesRead += bytesJustRead;
            return bytesJustRead;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            ValidateReadArguments(buffer, offset, count);
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeOffsetAsync(cancellationToken).ConfigureAwait(false);

            int bytesToRead = GetBytesToRead(count);
            if (bytesToRead == 0) return 0;

            int bytesJustRead = await mStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken).ConfigureAwait(false);
            mBytesRead += bytesJustRead;
            return bytesJustRead;
        }


        //private
        private int GetBytesToRead(int requestedCount) {
            if (mSourceExhaustedWhileSkipping) return 0;
            if (mMaxBytesToRead == -1) return requestedCount;
            return (int)Math.Min(requestedCount, BytesLeft);
        }
        private void InitializeOffset() {
            if (mOffsetInitialized) return;

            if (mStream.CanSeek) {
                mStream.Seek(mOffset, SeekOrigin.Begin);
                mBytesToSkip = 0;
            } else {
                SkipBytesReadAndDiscard();
            }
            mOffsetInitialized = true;
        }
        private async Task InitializeOffsetAsync(CancellationToken cancellationToken) {
            if (mOffsetInitialized) return;

            if (mStream.CanSeek) {
                mStream.Seek(mOffset, SeekOrigin.Begin);
                mBytesToSkip = 0;
            } else {
                await SkipBytesReadAndDiscardAsync(cancellationToken).ConfigureAwait(false);
            }
            mOffsetInitialized = true;
        }
        private void SkipBytesReadAndDiscard() {
            byte[] buffer = new byte[1024];
            while (mBytesToSkip > 0) {
                int read = mStream.Read(buffer, 0, (int)Math.Min(mBytesToSkip, buffer.Length));
                if (read == 0) {
                    mSourceExhaustedWhileSkipping = true;
                    mBytesToSkip = 0;
                    return;
                }
                mBytesToSkip -= read;
            }
        }
        private async Task SkipBytesReadAndDiscardAsync(CancellationToken cancellationToken) {
            byte[] buffer = new byte[1024];
            while (mBytesToSkip > 0) {
                int read = await mStream.ReadAsync(buffer, 0, (int)Math.Min(mBytesToSkip, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0) {
                    mSourceExhaustedWhileSkipping = true;
                    mBytesToSkip = 0;
                    return;
                }
                mBytesToSkip -= read;
            }
        }
        private static void ValidateReadArguments(byte[] buffer, int offset, int count) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count) throw new ArgumentException("Offset and count exceed the bounds of the buffer.");
        }
        private void ThrowIfDisposed() {
            if (mDisposed) throw new ObjectDisposedException(nameof(PartialInputStream));
        }

    }

}
