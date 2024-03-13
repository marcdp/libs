using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class Base64EncoderOutputStream : OutputStream {


        //variables
        private Stream mCryptoStream;


        //constructor
        public Base64EncoderOutputStream(Stream outputStream, bool leaveOpen = false) {
            if (leaveOpen) outputStream = new LeaveOpenOutputStream(outputStream);
            mCryptoStream = new CryptoStream(outputStream, new ToBase64Transform(), CryptoStreamMode.Write);
        }
        protected override void Dispose(bool disposing) {
            mCryptoStream.Dispose();
        }


        //methods		
        public override void Write(byte[] buffer, int offset, int count) {
            mCryptoStream.Write(buffer, offset, count);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await mCryptoStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
        public override void Flush() {
            mCryptoStream.Flush();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            await mCryptoStream.FlushAsync(cancellationToken);
        }


    }



}
