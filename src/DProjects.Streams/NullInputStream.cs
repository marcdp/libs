using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Threading;


namespace DProjects.Streams {


    public class NullInputStream : Stream {

        //props
        public override bool CanRead {
            get { return true; }
        }
        public override bool CanSeek {
            get { return false; }
        }
        public override bool CanWrite {
            get { return false; }
        }
        public override long Length {
            get { return 0; }
        }
        public override long Position {
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

        //methods
        public override int Read(byte[] buffer, int offset, int count) {
            return 0;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return Task.FromResult(0);
        }
        public override void Flush() {
        }
        public override Task FlushAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public override long Seek(long offset, SeekOrigin origin) {
            return 0;
        }
        public override void SetLength(long value) {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count) {
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }

}
