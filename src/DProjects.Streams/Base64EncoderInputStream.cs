using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class Base64EncoderInputStream : InputStream {


        //constants
        private static char[] CODES = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".ToCharArray();


        //variables
        private Stream mStream;
        private int mColumns;
        private bool mLeaveOpen;
        private byte[] mBuffer;
        private Queue<char> mQueue;
        private int mReaded;

        //constructor
        public Base64EncoderInputStream(Stream stream, int columns, bool leaveOpen = false) {
            mStream = stream;
            mColumns = ((int)columns / 4) * 4;
            mLeaveOpen = leaveOpen;
            mBuffer = new byte[3];
            mQueue = new Queue<char>();
        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                mStream.Dispose();
            }
        }


        //methods		
        public override int ReadByte() {
            if (mQueue.Count == 0) {
                int b1 = mStream.ReadByte();
                if (b1 == -1) return -1;
                int b2 = mStream.ReadByte();
                int b3 = mStream.ReadByte();

                mBuffer[0] = (byte)b1;
                mBuffer[1] = (byte)(b2 == -1 ? 0 : b2);
                mBuffer[2] = (byte)(b3 == -1 ? 0 : b3);

                mQueue.Enqueue(CODES[(mBuffer[0] & 0xFC) >> 2]);
                mQueue.Enqueue(CODES[((mBuffer[0] & 0x03) << 4) | ((mBuffer[1] & 0xF0) >> 4)]);
                if (b2 != -1) {
                    mQueue.Enqueue(CODES[((mBuffer[1] & 0x0F) << 2) | ((mBuffer[2] & 0xC0) >> 6)]);
                    if (b3 != -1) {
                        mQueue.Enqueue(CODES[mBuffer[2] & 0x3F]);
                    } else {
                        mQueue.Enqueue('=');
                    }
                } else {
                    mQueue.Enqueue('=');
                    mQueue.Enqueue('=');
                }
                mReaded += 4;
                if (mColumns != 0 && mReaded >= mColumns) {
                    mReaded = 0;
                    mQueue.Enqueue('\n');
                }
            }
            return mQueue.Dequeue();
        }
        public override int Read(byte[] buffer, int offset, int count) {
            int bytes = 0;
            for (int i = offset; i < offset + count; i++) {
                int b = ReadByte();
                if (b == -1) break;
                buffer[i] = (byte)b;
                bytes++;
            }
            return bytes;
        }

        //async 
        public async Task<int> ReadByteAsync(CancellationToken cancellationToken) {
            if (mQueue.Count == 0) {
                var readTotal = 0;
                mBuffer[0] = 0;
                mBuffer[1] = 0;
                mBuffer[2] = 0;
                do {
                    int read = await mStream.ReadAsync(mBuffer, readTotal, 3 - readTotal, cancellationToken);
                    if (read == 0) break;
                    readTotal += read;
                } while (readTotal != 3);
                if (readTotal == 0) return -1;

                int b1 = mBuffer[0];
                int b2 = mBuffer[1];
                int b3 = mBuffer[2];
                if (readTotal == 1) {
                    b2 = -1;
                    b3 = -1;
                } else if (readTotal == 2) {
                    b3 = -1;
                }
                mQueue.Enqueue(CODES[(mBuffer[0] & 0xFC) >> 2]);
                mQueue.Enqueue(CODES[((mBuffer[0] & 0x03) << 4) | ((mBuffer[1] & 0xF0) >> 4)]);
                if (b2 != -1) {
                    mQueue.Enqueue(CODES[((mBuffer[1] & 0x0F) << 2) | ((mBuffer[2] & 0xC0) >> 6)]);
                    if (b3 != -1) {
                        mQueue.Enqueue(CODES[mBuffer[2] & 0x3F]);
                    } else {
                        mQueue.Enqueue('=');
                    }
                } else {
                    mQueue.Enqueue('=');
                    mQueue.Enqueue('=');
                }
                mReaded += 4;
                if (mColumns != 0 && mReaded >= mColumns) {
                    mReaded = 0;
                    mQueue.Enqueue('\n');
                }
            }
            return mQueue.Dequeue();
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int bytes = 0;
            for (int i = offset; i < offset + count; i++) {
                int b = await ReadByteAsync(cancellationToken);
                if (b == -1) break;
                buffer[i] = (byte)b;
                bytes++;
            }
            return bytes;
        }


    }



}
