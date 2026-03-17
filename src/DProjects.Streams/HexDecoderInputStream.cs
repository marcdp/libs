using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


namespace DProjects.Streams {


    public class HexDecoderInputStream : InputStream {


        //variables
        private TextReader mReader;
        private char[] mBuffer;

        //constructor
        public HexDecoderInputStream(Stream inputStream, bool leaveOpen = false) {
            mReader = new StreamReader(inputStream, System.Text.Encoding.ASCII, false, 1024, leaveOpen);
            mBuffer = new char[2];
        }
        protected override void Dispose(bool disposing) {
            mReader.Close();
            mReader.Dispose();
        }


        //methods		
        public override int Read(byte[] buffer, int offset, int count) {
            int bytes = 0;
            for (int i = offset; i < offset + count; i++) {
                int b0 = mReader.Read();
                while (b0 == 10 || b0 == 13) b0 = mReader.Read();
                if (b0 == -1) break;
                int b1 = mReader.Read();
                if (b1 == -1) throw new InvalidDataException("Invalid hex input");

                var high = HexToInt((char)b0) << 4;
                var low = HexToInt((char)b1);

                buffer[i] = (byte)(high | low);
                bytes++;
            }
            return bytes;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int bytes = 0;
            for (int i = offset; i < offset + count; i++) {
                var readed = await mReader.ReadAsync(mBuffer, 0, 1);
                if (readed == 0) break;
                var b0 = mBuffer[0];
                while ((b0 == 10 || b0 == 13) && readed != 0) {
                    readed = await mReader.ReadAsync(mBuffer, 0, 1);
                }
                if (readed == 0) break;

                readed = await mReader.ReadAsync(mBuffer, 1, 1);
                if (readed == 0) break;
                var b1 = mBuffer[1];
                while ((b1 == 10 || b1 == 13) && readed != 0) {
                    readed = await mReader.ReadAsync(mBuffer, 1, 1);
                }
                if (readed == 0) break;

                var high = HexToInt((char)b0) << 4;
                var low = HexToInt((char)b1);

                buffer[i] = (byte)(high | low);
                bytes++;
            }
            return bytes;
        }
        private static int HexToInt(char c) {
            switch (c) {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a': case 'A': return 10;
                case 'b': case 'B': return 11;
                case 'c': case 'C': return 12;
                case 'd': case 'D': return 13;
                case 'e': case 'E': return 14;
                case 'f': case 'F': return 15;
                default: throw new FormatException("Unrecognized hex char " + c);
            }
        }

    }



}
