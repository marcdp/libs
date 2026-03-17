using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class LeaveOpenOutputStream : OutputStream {


        //variables
        private Stream mStream;


        //construcotr
        public LeaveOpenOutputStream(Stream stream) {
            mStream = stream;
        }


        //methods
        public override void Write(byte[] buffer, int offset, int count) {
            mStream.Write(buffer, offset, count);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            await mStream.WriteAsync(buffer, offset, count);
        }
        public override void Flush() {
            mStream.Flush();
        }
        public override async Task FlushAsync(CancellationToken cancellationToken) {
            await mStream.FlushAsync(cancellationToken);
        }
        
    }


}
