using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class LimitedInputStream : InputStream {


        //variables
        private Stream mStream;

        private long mMaxBytes;
        private long mBytesRead;

        private bool mLeaveOpen;
        private bool mDisposed;


        //constructor
        public LimitedInputStream(Stream stream, long maxBytes, bool leaveOpen = false) {
            mStream = stream;
            mMaxBytes = maxBytes;
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
        //public override bool CanRead { get { return mStream.CanRead; } }
        //public override bool CanSeek { get { return false; } }
        //public override bool CanWrite { get { return false; } }
        //public override long Position { get { return mStream.Position; } set { mStream.Position = value; } }
        public override long Length { get { return Math.Min( mStream.Length, mMaxBytes); } }
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
        public override int Read(byte[] buffer, int offset, int count) {
            if (mBytesRead >= mMaxBytes) return 0;
            if (offset + count > mMaxBytes) count = (int) mMaxBytes - offset;
            int bytesJustRead = mStream.Read(buffer, offset, count);
            mBytesRead += bytesJustRead;
            return bytesJustRead;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (mBytesRead >= mMaxBytes) return 0;
            if (offset + count > mMaxBytes) count = (int)mMaxBytes - offset;
            int bytesJustRead = await mStream.ReadAsync(buffer, offset, count);
            mBytesRead += bytesJustRead;
            return bytesJustRead;
        }
        
         

    }

}
