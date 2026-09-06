using System;
using System.IO;
using System.Threading;


namespace DProjects.Streams {


    public abstract class OutputStream : Stream {


        //constructor
        public OutputStream() {
        }


        //properties
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException("This stream does not support seeking.");
        public override long Position {
            get { throw new NotSupportedException("This stream does not support seeking."); }
            set { throw new NotSupportedException("This stream does not support seeking."); }
        }


        //methods
        public override void SetLength(long value) {
            throw new NotSupportedException("This stream does not support seeking.");
        }
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("This stream does not support reading.");
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("This stream does not support seeking.");
        }


    }


}
