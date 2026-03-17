using DProjects.Factories.Attributes;
using DProjects.Utils;
using DProjects.Streams;
using System;
using System.IO;

namespace DProjects.Crypto {


    [Protocol("caesar", "")]
    [ProtocolExample("caesar:", "")]
    public class CryptoSymmetricDecryptCaesar : ICryptoSymmetricDecrypt {

        //options
        public class Options {
            public char Separator { get; set; } = ',';
            public bool Header { get; set; } = true;
            public override string ToString() {
                return UrlUtils.Serialize("caesar", this);
            }
        }


        //variables
        private Options mOptions;


        //constructor
        public CryptoSymmetricDecryptCaesar(Options options) {
            mOptions = options;
        }
        public void Dispose() {
        }


        //methods
        public Stream GetStream(Stream input, string password) {
            return GetStream(input, (version) => password);
        }
        public Stream GetStream(Stream input, Func<string, string> passwordProvider) {
            //header
            var optionsToUse = mOptions;
            if (mOptions.Header) {
                var header = StreamUtils.ReadLine(input, System.Text.Encoding.UTF8, mOptions.Separator, 512);
                if (header == null) throw new Exception("Unable to decrypt: invalid header: null");
                optionsToUse = UrlUtils.Deserialize<Options>(header);
            }
            //key
            var password = passwordProvider("");
            int key = int.Parse(password);
            //return 
            return new CaesarDecryptStream(input, key);
        }


        //stream
        private class CaesarDecryptStream : InputStream {
            private Stream mInnerStream;
            private int mKey;
            public CaesarDecryptStream(Stream innerStream, int key) {
                mInnerStream = innerStream;
                mKey = key;
            }
            public override int Read(byte[] buffer, int offset, int count) {
                int bReaded = mInnerStream.Read(buffer, offset, count);
                for(var i = offset; i < offset + bReaded; i++) {
                    var b = buffer[i];
                    var bNew = (byte)((b - mKey) % 256);
                    buffer[i] = bNew;
                }
                return bReaded;
            }
        }

    }

}
