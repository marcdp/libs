using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.IO;
using System.Security.Cryptography;
using static DProjects.Crypto.CryptoSymmetricEncryptAES;
using DProjects.Factories;
using DProjects.Streams;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DProjects.Crypto {

    [Protocol("aes", "")]
    [ProtocolUsage("")]
    [ProtocolExample("aes:?encoding=binary", "")]
    [ProtocolExample("aes:?encoding=base64&cipher=ECB&ivLength=32", "")]
    public class CryptoSymmetricDecryptAESFactory : IFactoryByUrl<ICryptoSymmetricDecrypt> {
        public ICryptoSymmetricDecrypt Create(string src) {
            return new CryptoSymmetricDecryptAES(UrlUtils.Deserialize<CryptoSymmetricDecryptAES.Options>(src, new() {
                ThrowExceptionIfPropertyNotFound = false
            }));
        }
    }



    [Protocol("aes", "")]
    [ProtocolUsage("")]
    [ProtocolExample("aes:?encoding=binary", "")]
    [ProtocolExample("aes:?encoding=base64&cipher=ECB&ivLength=32", "")]
    public class CryptoSymmetricDecryptAES : ICryptoSymmetricDecrypt {

        //enums
        public enum Encoding {
            Base64,
            Binary
        }

        //options
        public class Options {
            public int Iterations { get; set; } = 0;
            public int SaltLength { get; set; } = 16;
            public int IVLength { get; set; } = 16;
            public int BlockSize { get; set; } = 16;
            public PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;
            public CipherMode CipherMode { get; set; } = CipherMode.CBC;
            public int KeySize { get; set; } = 32;
            public Encoding Encoding { get; set; } = Encoding.Base64;
            public char Separator { get; set; } = ',';
            public string Version { get; set; } = "";
            public bool Header { get; set; } = true;            
        }


        //variables
        private Options mOptions;


        //constructor
        public CryptoSymmetricDecryptAES() {
            mOptions = new Options();
        }
        public CryptoSymmetricDecryptAES(Options options) {
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
            //get password
            var password = passwordProvider(optionsToUse.Version);
            //prepare
            if (optionsToUse.Encoding == Encoding.Base64) {
                //base64
                var msb = new Base64DecoderInputStream(input, true);
                var iv = StreamUtils.ReadBytes(msb, optionsToUse.IVLength);
                var salt = StreamUtils.ReadBytes(msb, optionsToUse.SaltLength);
                var iterations4 = StreamUtils.ReadBytes(msb, 4);
                var iterations = BitConverter.ToInt32(iterations4, 0);
                var key = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, optionsToUse.KeySize);
                var aes = System.Security.Cryptography.Aes.Create();
                aes.Mode = optionsToUse.CipherMode;
                aes.Padding = optionsToUse.PaddingMode;
                aes.BlockSize = optionsToUse.BlockSize * 8;
                aes.KeySize = optionsToUse.KeySize * 8;
                aes.Key = key;
                aes.IV = iv;
                //cryptoStream
                var decryptor = aes.CreateDecryptor();
                var cryptoStream = new CryptoStream(new LeaveOpenInputStream(msb), decryptor, CryptoStreamMode.Read);
                //skip firt SaltLength * 1.5 bytes (salt encoded in base64)
                StreamUtils.ReadBytes(cryptoStream, (int)(optionsToUse.SaltLength * 1.5));
                //return
                return cryptoStream;
            } else if (optionsToUse.Encoding == Encoding.Binary) {
                //binary
                var iv = StreamUtils.ReadBytes(input, optionsToUse.IVLength);
                var salt = StreamUtils.ReadBytes(input, optionsToUse.SaltLength);
                var iterations4 = StreamUtils.ReadBytes(input, 4);
                var iterations = BitConverter.ToInt32(iterations4, 0);
                var key = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, optionsToUse.KeySize);
                var aes = System.Security.Cryptography.Aes.Create();
                aes.Mode = optionsToUse.CipherMode;
                aes.Padding = optionsToUse.PaddingMode;
                aes.BlockSize = optionsToUse.BlockSize * 8;
                aes.KeySize = optionsToUse.KeySize * 8;
                aes.Key = key;
                aes.IV = iv;
                //cryptoStream
                var decryptor = aes.CreateDecryptor();
                var cryptoStream = new CryptoStream(new LeaveOpenInputStream(input), decryptor, CryptoStreamMode.Read);
                //firt 16 bytes (salt16)
                StreamUtils.ReadBytes(cryptoStream, optionsToUse.SaltLength);
                //return
                return cryptoStream;
            } else {
                throw new NotImplementedException();
            }

        } 

    }


}
