using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class CatInputStream : InputStream {


        //variables
        private Queue<Stream> mStreams;
        private bool mLeaveOpen;
        private long mPosition;


        //constructor
        public CatInputStream(IEnumerable<Stream> streams, bool leaveOpen = false) {
            mStreams = new Queue<Stream>(streams);
            mLeaveOpen = leaveOpen;

        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                while (mStreams.Count > 0) {
                    mStreams.Dequeue().Dispose();
                }
            }
        }
         

        //methods
        public override int Read(byte[] buffer, int offset, int count) {
            if (mStreams.Count == 0) {
                return 0;
            }
            int bytesRead = mStreams.Peek().Read(buffer, offset, count);
            mPosition += bytesRead;
            if (bytesRead == 0) {
                if (!mLeaveOpen) {
                    mStreams.Dequeue().Dispose();
                } else {
                    mStreams.Dequeue();
                }
                bytesRead += Read(buffer, offset + bytesRead, count - bytesRead);
                mPosition += bytesRead;
            }
            if (bytesRead < count) {
                bytesRead += Read(buffer, offset + bytesRead, count - bytesRead);
            }
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (mStreams.Count == 0) return 0;
            int bytesRead = await mStreams.Peek().ReadAsync(buffer, offset, count, cancellationToken);
            mPosition += bytesRead;
            if (bytesRead == 0) {
                if (!mLeaveOpen) {
                    mStreams.Dequeue().Dispose();
                } else {
                    mStreams.Dequeue();
                }
                bytesRead += await ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
                mPosition += bytesRead;
            }
            if (bytesRead < count) {
                bytesRead += await ReadAsync(buffer, offset + bytesRead, count - bytesRead, cancellationToken);
            }
            return bytesRead;
        }



    }


}
