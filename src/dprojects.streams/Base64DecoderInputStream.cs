using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class Base64DecoderInputStream : InputStream {


        //constants
        private static string CODES = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        private static byte[] BYTES_TO_SKIP = [13, 10];


        //variables
        private Stream mStream;
        private bool mLeaveOpen;
        private int[] mBuffer;
        private byte[] mByteBuffer;
        private Queue<byte> mQueue;


        //constructor
        public Base64DecoderInputStream(Stream stream, bool leaveOpen = false) {
            mStream = stream;
            mLeaveOpen = leaveOpen;
            mBuffer = new int[4];
            mByteBuffer = new byte[4];
            mQueue = new Queue<byte>();
        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                mStream.Dispose();
            }
        }


        //methods		
        private int ReadBytePrivate() {
            int b = mStream.ReadByte();
            while (b == '\r' || b == '\n') b = mStream.ReadByte();
            return b;
        }
        public override int ReadByte() {
            if (mQueue.Count == 0) {
                //read buffer
                if (!ReadBuffer(mStream, mByteBuffer, 0, 4, BYTES_TO_SKIP)) return -1;
                mBuffer[0] = CODES.IndexOf((char)mByteBuffer[0]);
                mBuffer[1] = CODES.IndexOf((char)mByteBuffer[1]);
                mBuffer[2] = CODES.IndexOf((char)mByteBuffer[2]);
                mBuffer[3] = CODES.IndexOf((char)mByteBuffer[3]);

                //decode
                mQueue.Enqueue((byte)((mBuffer[0] << 2) | (mBuffer[1] >> 4)));
                if (mBuffer[2] < 64) {
                    mQueue.Enqueue((byte)((mBuffer[1] << 4) | (mBuffer[2] >> 2)));
                    if (mBuffer[3] < 64) {
                        mQueue.Enqueue((byte)((mBuffer[2] << 6) | mBuffer[3]));
                    }
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
                //read
                if (! await ReadBufferAsync(mStream, mByteBuffer, 0, 4, BYTES_TO_SKIP, cancellationToken)) return -1;
                mBuffer[0] = CODES.IndexOf((char)mByteBuffer[0]);
                mBuffer[1] = CODES.IndexOf((char)mByteBuffer[1]);
                mBuffer[2] = CODES.IndexOf((char)mByteBuffer[2]);
                mBuffer[3] = CODES.IndexOf((char)mByteBuffer[3]);
                //decode
                mQueue.Enqueue((byte)((mBuffer[0] << 2) | (mBuffer[1] >> 4)));
                if (mBuffer[2] < 64) {
                    mQueue.Enqueue((byte)((mBuffer[1] << 4) | (mBuffer[2] >> 2)));
                    if (mBuffer[3] < 64) {
                        mQueue.Enqueue((byte)((mBuffer[2] << 6) | mBuffer[3]));
                    }
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


        //private
        public static bool ReadBuffer(Stream stream, byte[] buffer, int offset, int length, byte[] excluded) {
            //fill buffer, skiping certain characters (ex: 13, 10)
            var readedTotal = 0;
            do {
                int readed = stream.Read(buffer, offset + readedTotal, length - readedTotal);
                if (readed == 0) break;
                readedTotal += readed;
            } while (readedTotal != length);
            if (readedTotal == 0) return false;
            for (var i = offset; i < offset + length; i++) {
                if (excluded.Contains(buffer[i])) {
                    for (var k = i; k < offset + length - 1; k++) buffer[k] = buffer[k + 1];
                    buffer[offset + length - 1] = 0;
                    readedTotal--;
                }
            }
            if (readedTotal != length) {
                return ReadBuffer(stream, buffer, offset + readedTotal, length - readedTotal, excluded);
            }
            if (readedTotal != length) throw new Exception("Error reading from stream, expected bytes, but received less: " + readedTotal);
            return true;
        }
        public static async Task<bool> ReadBufferAsync(Stream stream, byte[] buffer, int offset, int length, byte[] excluded, CancellationToken cancellationToken) {
            //fill buffer, skiping certain characters (ex: 13, 10)
            var readedTotal = 0;
            do {
                int readed = await stream.ReadAsync(buffer, offset + readedTotal, length - readedTotal, cancellationToken);
                if (readed == 0) break;
                readedTotal += readed;
            } while (readedTotal != length);
            if (readedTotal == 0) return false;
            for (var i = offset; i < offset + length; i++) {
                if (excluded.Contains(buffer[i])) {
                    for (var k = i; k < offset + length - 1; k++) buffer[k] = buffer[k + 1];
                    buffer[offset + length - 1] = 0;
                    readedTotal--;
                }
            }
            if (readedTotal != length) {
                return await ReadBufferAsync(stream, buffer, offset + readedTotal, length - readedTotal, excluded, cancellationToken);
            }
            if (readedTotal != length) throw new Exception("Error reading from stream, expected bytes, but received less: " + readedTotal);
            return true;
        }

    }



}
