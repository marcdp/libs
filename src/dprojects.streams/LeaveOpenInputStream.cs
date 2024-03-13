using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class LeaveOpenInputStream : InputStream {


        //variables
        private Stream mStream;


        //constructor
        public LeaveOpenInputStream(Stream stream) {
            mStream = stream;
        }


        //props
        public override bool CanRead {
            get { return mStream.CanRead; }
        }
        public override bool CanSeek {
            get { return mStream.CanSeek; }
        }
        public override bool CanWrite {
            get { return mStream.CanWrite; }
        }
        public override long Length {
            get { return mStream.Length; }
        }
        public override long Position {
            get => mStream.Position; 
            set => mStream.Position = value;
        }

        //methods
        public override int Read(byte[] buffer, int offset, int count) {
            return mStream.Read(buffer, offset, count);
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return await mStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override void SetLength(long value) {
            mStream.SetLength(value);
        }
        public override long Seek(long offset, SeekOrigin origin) {
            return mStream.Seek(offset, origin);
        }
    }


}
