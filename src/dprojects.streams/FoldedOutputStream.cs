using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Streams {


    public class FoldedOutputStream : Stream {


        //variables
        private Stream mStream;
        private bool mLeaveOpen;
        private int mBytesPerLine;
        private int mAux;

        //constructor
        public FoldedOutputStream(Stream stream, int bytesPerLine = 76, bool leaveOpen = false) {
            mStream = stream;
            mLeaveOpen = leaveOpen;
            mBytesPerLine = bytesPerLine;
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (!mLeaveOpen) {
                mStream.Dispose();
            }
        }


        //properties
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
            get { return mStream.Length; }
        }
        public override long Position {
            get { return mStream.Position; }
            set { throw new NotImplementedException(); }
        }


        //methods
        public override void Flush() {
            mStream.Flush();
        }
        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }
        public override void SetLength(long value) {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count) {
            while (count > 0) {
                var length = Math.Min(count, mBytesPerLine - mAux);
                mStream.Write(buffer, offset, length);
                offset += length;
                count -= length;
                mAux += length;
                if (mAux == mBytesPerLine) {
                    mAux = 0;
                    mStream.WriteByte(10);
                }
            }
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            while (count > 0) {
                var length = Math.Min(count, mBytesPerLine - mAux);
                await mStream.WriteAsync(buffer, offset, length, cancellationToken);
                offset += length;
                count -= length;
                mAux += length;
                if (mAux == mBytesPerLine) {
                    mAux = 0;
                    mStream.WriteByte(10);
                }
            }
        }


    }



}
