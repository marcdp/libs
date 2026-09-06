using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class GZipCompressOutputStream : Stream, IAsyncDisposable {


        //variables
        private Stream mOutputStream;
        private bool mLeaveOpen;
        private Stream mGZipStream;


        //constructor
        public GZipCompressOutputStream(Stream outputStream, bool leaveOpen = false) {
            mOutputStream = outputStream;
            mLeaveOpen = leaveOpen;
            mGZipStream = new System.IO.Compression.GZipStream(mOutputStream, System.IO.Compression.CompressionMode.Compress, true);
        }
        protected override void Dispose(bool disposing) {
            mGZipStream.Dispose();
            if (!mLeaveOpen) {
                mOutputStream.Dispose();
            }
        }
        public async ValueTask DisposeAsync() {
            await mGZipStream.FlushAsync();
            mGZipStream.Dispose();
            if (!mLeaveOpen) {
                mOutputStream.Dispose();
            }
        }


        //properties
        public override bool CanRead {
            get { return mGZipStream.CanRead; }
        }
        public override bool CanSeek {
            get { return mGZipStream.CanSeek; }
        }
        public override bool CanWrite {
            get { return mGZipStream.CanWrite; }
        }
        public override long Length {
            get { throw new NotSupportedException("This stream does not support seeking."); }
        }
        public override long Position {
            get { throw new NotSupportedException("This stream does not support seeking."); }
            set { throw new NotSupportedException("This stream does not support seeking."); }
        }


        //methods
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("This stream does not support reading.");
        }
        
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void SetLength(long value) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void Write(byte[] buffer, int offset, int count) {
            mGZipStream.Write(buffer, offset, count);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await mGZipStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
        public override void Flush() {
            mGZipStream.Flush();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            await mGZipStream.FlushAsync(cancellationToken);
        }

    }


}
