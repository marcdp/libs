using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Streams {


    public class HexEncoderInputStream : InputStream {


        //variables
        private Stream mInputStream;
        private bool mLeaveOpen;
        private byte[] mBuffer;


        //constructor
        public HexEncoderInputStream(Stream inputStream, bool leaveOpen = false) {
            mInputStream = new BufferedStream(inputStream, 1024);
            mLeaveOpen = leaveOpen;
            mBuffer = new byte[1];
        }
        protected override void Dispose(bool disposing) {
            if (!mLeaveOpen) {
                mInputStream.Dispose();
            }
        }


        //methods		
        public override int Read(byte[] buffer, int offset, int count) {
            int bytes = 0;
            for (int i = offset; i + 1 < offset + count; i += 2) {
                int b = mInputStream.ReadByte();
                if (b == -1) break;
                var hexOutput = BitConverter.ToString(new byte[] { (byte)b }).ToCharArray();
                buffer[i] = (byte)IntToChar(b >> 4);
                buffer[i + 1] = (byte)IntToChar(b & 0x0F);
                bytes += 2;
            }
            return bytes;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            int bytes = 0;
            for (int i = offset; i + 1 < offset + count; i += 2) {
                var readed = await mInputStream.ReadAsync(mBuffer, 0, 1, cancellationToken);
                if (readed == 0) break;
                var b = mBuffer[0];
                var hexOutput = BitConverter.ToString(mBuffer).ToCharArray();
                buffer[i] = (byte)IntToChar(b >> 4);
                buffer[i + 1] = (byte)IntToChar(b & 0x0F);
                bytes += 2;
            }
            return bytes;
        }
        private static char IntToChar(int i) {
            switch (i) {
                case 0: return '0';
                case 1: return '1';
                case 2: return '2';
                case 3: return '3';
                case 4: return '4';
                case 5: return '5';
                case 6: return '6';
                case 7: return '7';
                case 8: return '8';
                case 9: return '9';
                case 10: return 'a';
                case 11: return 'b';
                case 12: return 'c';
                case 13: return 'd';
                case 14: return 'e';
                case 15: return 'f';
                default: throw new FormatException("Unrecognized hex value " + i);
            }
        }

    }



}
