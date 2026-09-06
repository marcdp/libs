using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class GZipDecompressInputStream : Stream {


        //variables
        private Stream mInputStream;
        private bool mLeaveOpen;
        private Stream mGZipStream;


        //constructor
        public GZipDecompressInputStream(Stream inputStream, bool leaveOpen = false) {
            mInputStream = inputStream;
            mLeaveOpen = leaveOpen;
            mGZipStream = new System.IO.Compression.GZipStream(mInputStream, System.IO.Compression.CompressionMode.Decompress);
        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                mInputStream.Dispose();
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
            return mGZipStream.Read(buffer, offset, count);
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return await mGZipStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override void Flush() {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void SetLength(long value) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("This stream does not support writing.");
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            throw new NotSupportedException("This stream does not support writing.");
        }


    }



}
