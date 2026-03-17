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
        public override long Length => 0;
        public override long Position {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        //methods
        public override void SetLength(long value) {
            throw new NotImplementedException();
        }
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }


    }


}
