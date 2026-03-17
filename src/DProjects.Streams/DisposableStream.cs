using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class DisposableStream : Stream {

        //variables
        private Stream mStream ;
        private bool mLeaveOpen;
        private Action mOnDispose;
        private bool mDisposed = false;


        //constructor
        public DisposableStream(Stream stream, Action onDispose, bool leaveOpen = false) {
            mStream = stream;
            mOnDispose = onDispose;
            mLeaveOpen = leaveOpen;
        }
        protected override void Dispose(bool disposing) {
            if (!mDisposed) {
                mDisposed = true;
                mOnDispose();
                if (!mLeaveOpen) mStream.Dispose();
            }
            base.Dispose(disposing);
        }


        //methods
        public override bool CanRead { get { return mStream.CanRead; } }
        public override bool CanSeek { get { return mStream.CanSeek; } }
        public override bool CanWrite { get { return mStream.CanWrite; } }
        public override long Position { get { return mStream.Position; } set { mStream.Position = value; } }
        public override long Length { get { return mStream.Length; } }


        //methods
        public override long Seek(long offset, SeekOrigin origin) {
            return mStream.Seek(offset, origin);
        }
        public override int ReadByte() {
            return mStream.ReadByte();
        }
        public override int Read(byte[] buffer, int offset, int count) {
            return mStream.Read(buffer, offset, count);
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return await mStream.ReadAsync(buffer, offset, count, cancellationToken);
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
        public override void Write(byte[] buffer, int offset, int count) {
            mStream.Write(buffer, offset, count);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await mStream.WriteAsync(buffer, offset, count, cancellationToken );
        }
    }



}
