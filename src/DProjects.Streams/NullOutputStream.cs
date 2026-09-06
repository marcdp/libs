using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;


namespace DProjects.Streams {


    public class NullOutputStream : Stream {


        //props
        public override bool CanRead {
            get { return false; }
        }
        public override bool CanSeek {
            get { return false; }
        }
        public override bool CanWrite {
            get { return true; }
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
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            throw new NotSupportedException("This stream does not support reading.");
        }
        public override void Flush() {
        }
        public override Task FlushAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void SetLength(long value) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override void Write(byte[] buffer, int offset, int count) {
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


    }


}
