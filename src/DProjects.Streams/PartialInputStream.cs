using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class PartialInputStream : Stream {


        //variables
        private Stream mStream;

        private long mOffset;
        private bool mOffsetInitialized;
        private long mMaxBytesToRead;
        private long mBytesRead;

        private bool mLeaveOpen;
        private bool mDisposed;


        //constructor
        public PartialInputStream(Stream stream, long offset, long length, bool leaveOpen = false) {
            mStream = stream;
            mOffset = offset;
            mMaxBytesToRead = length;
            mLeaveOpen = leaveOpen;
        }
        protected override void Dispose(bool disposing) {
            if (!mDisposed) {
                mDisposed = true;
                if (!mLeaveOpen) mStream.Dispose();
            }
            base.Dispose(disposing);
        }


        //methods
        public override bool CanRead { get { return mStream.CanRead; } }
        public override bool CanSeek { get { return mStream.CanSeek; } }
        public override bool CanWrite { get { return mStream.CanWrite; } }
        public override long Position { get { return mStream.Position; } set { mStream.Position = value; } }
        public override long Length { get { return mStream.Length; } }
        public long BytesLeft {
            get {
                if (mMaxBytesToRead == -1) throw new NotImplementedException();
                return mMaxBytesToRead - mBytesRead;
            }
        }
        public long BytesRead => mBytesRead;


        //methods
        public override long Seek(long offset, SeekOrigin origin) {
            return mStream.Seek(offset, origin);
        }
        public override void Flush() {
            mStream.Flush();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            await mStream.FlushAsync(cancellationToken);
        }
        public override void SetLength(long value) {
            mStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count) {
            mStream.Write(buffer, offset, count);
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        public override int Read(byte[] buffer, int offset, int count) {
            //offset
            if (!mOffsetInitialized) {
                if (mOffset> 0) {
                    if (mStream.CanSeek) {
                        mStream.Seek(mOffset, SeekOrigin.Begin);
                    } else {
                        SkipBytesReadAndDiscard(mOffset);
                    }
                }
                mOffsetInitialized = true;
            }
            //read
            if (mMaxBytesToRead == -1) {
                int bytesJustRead = mStream.Read(buffer, offset, count);
                mBytesRead += bytesJustRead;
                return bytesJustRead;
            } else {
                if (mBytesRead >= mMaxBytesToRead) return 0;
                long bytesLeft_Renamed = BytesLeft;
                if (count > bytesLeft_Renamed) {
                    count = (int)bytesLeft_Renamed;
                }
                int bytesJustRead = mStream.Read(buffer, offset, count);
                mBytesRead += bytesJustRead;
                return bytesJustRead;
            }
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            //offset
            if (!mOffsetInitialized) {
                if (mOffset > 0) {
                    if (mStream.CanSeek) {
                        mStream.Seek(mOffset, SeekOrigin.Begin);
                    } else {
                        await SkipBytesReadAndDiscardAsync(mOffset, cancellationToken);
                    }
                }
                mOffsetInitialized = true;
            }
            //read
            if (mMaxBytesToRead == -1) {
                int bytesJustRead = await mStream.ReadAsync(buffer, offset, count);
                mBytesRead += bytesJustRead;
                return bytesJustRead;
            } else {
                if (mBytesRead >= mMaxBytesToRead) return 0;
                long bytesLeft_Renamed = BytesLeft;
                if (count > bytesLeft_Renamed) {
                    count = (int)bytesLeft_Renamed;
                }
                int bytesJustRead = await mStream.ReadAsync(buffer, offset, count);
                mBytesRead += bytesJustRead;
                return bytesJustRead;
            }
        }
        

        //private
        private void SkipBytesReadAndDiscard(long bytesToSkip) {
            byte[] buffer = new byte[1024]; // Adjustable buffer size
            long remaining = bytesToSkip;
            while (remaining > 0) {
                int read = mStream.Read(buffer, 0, (int)Math.Min(remaining, buffer.Length));
                remaining -= read;
            }
        }
        private async Task SkipBytesReadAndDiscardAsync(long bytesToSkip, CancellationToken cancellationToken) {
            byte[] buffer = new byte[1024]; // Adjustable buffer size
            long remaining = bytesToSkip;
            while (remaining > 0) {
                int read = await mStream.ReadAsync(buffer, 0, (int)Math.Min(remaining, buffer.Length), cancellationToken);
                remaining -= read;
            }
        }

    }

}
