using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class ByteCounterInputStream : Stream {

        //variables
        private Stream mStream;
        private bool mLeaveOpen;
        private long mCount;


        //constructor
        public ByteCounterInputStream(Stream stream, bool leaveOpen = false) {
            mStream = stream;
            mLeaveOpen = leaveOpen;
            mCount = 0;
        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                mStream.Dispose();
            }
        }


        //properties
        public override bool CanRead => mStream.CanRead;
        public override bool CanSeek => mStream.CanSeek;
        public override bool CanWrite => mStream.CanWrite;
        public override long Length => mStream.Length;
        public override long Position {
            get { return mStream.Position; }
            set { mStream.Position = value; }
        }
        public long Count => mCount;


        //methods
        public override int Read(byte[] buffer, int offset, int count) {
            int c = mStream.Read(buffer, offset, count);
            if (c != -1) {
                mCount += c;
            }
            return c;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int c = await mStream.ReadAsync(buffer, offset, count);
            if (c != -1) {
                mCount += c;
            }
            return c;
        }
        public override void Flush() {
            mStream.Flush();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            await mStream.FlushAsync(cancellationToken);
        }
        public override long Seek(long offset, SeekOrigin origin) {
            return mStream.Seek(offset, origin);
        }
        public override void SetLength(long value) {
            mStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count) {
            mStream.Write(buffer, offset, count);
            mCount += count;
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await mStream.WriteAsync(buffer, offset, count, cancellationToken);
            mCount += count;
        }


    }


}
