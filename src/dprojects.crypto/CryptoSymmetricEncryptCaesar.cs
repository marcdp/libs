using DProjects.Factories.Attributes;
using DProjects.Streams;
using DProjects.Utils;
using System;
using System.IO;
using System.Security.Cryptography;

namespace DProjects.Crypto {


    [Protocol("caesar", "")]
    [ProtocolExample("caesar:", "")]
    public class CryptoSymmetricEncryptCaesar : ICryptoSymmetricEncrypt {

        //options
        public class Options {
            public char Separator { get; set; } = ',';
            public bool Header { get; set; } = true;            
        }


        //variables
        private Options mOptions;


        //constructor
        public CryptoSymmetricEncryptCaesar(Options options) {
            mOptions = options;
        }
        public void Dispose() {
        }


        //methods
        public Stream GetStream(Stream output, string password) {
            //key
            int key = int.Parse(password);
            //header
            if (mOptions.Header) {
                var header = UrlUtils.Serialize("caesar", mOptions, new() {
                    Excluded = new string[] { "Separator" }
                });
                var headerBuffer = System.Text.Encoding.UTF8.GetBytes(header + mOptions.Separator);
                output.Write(headerBuffer, 0, headerBuffer.Length);
            }
            //return 
            return new CaesarEncryptStream(output, key);
        }


        //stream
        private class CaesarEncryptStream : OutputStream {
            private Stream mInnerStream;
            private int mKey;
            public CaesarEncryptStream(Stream innerStream, int key) {
                mInnerStream = innerStream;
                mKey = key;
            }
            public override void Write(byte[] buffer, int offset, int count) {
                for(var i=0; i<count; i++) {
                    var b = buffer[offset + i];
                    var bNew = (byte) ((b + mKey) % 256);
                    mInnerStream.WriteByte(bNew);
                }
            }
            public override void Flush() {
                mInnerStream.Flush();
            }
        }

    }

}
