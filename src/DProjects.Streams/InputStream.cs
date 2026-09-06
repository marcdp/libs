using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public abstract class InputStream : Stream {


        //constructor
        public InputStream() {
        }


        //properties
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException("This stream does not support seeking.");
        public override long Position {
            get { throw new NotSupportedException("This stream does not support seeking."); }
            set { throw new NotSupportedException("This stream does not support seeking."); }
        }


        //methods
        public override void Flush() {
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
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }


    }


}
