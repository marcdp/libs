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
    [ProtocolExample("aes:?encoding=base64&cipherMode=CBC&ivLength=16", "")]
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
    [ProtocolExample("aes:?encoding=base64&cipherMode=CBC&ivLength=16", "")]
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
            ValidateOptions(optionsToUse);
            //get password
            var password = passwordProvider(optionsToUse.Version);
            //prepare
            if (optionsToUse.Encoding == Encoding.Base64) {
                //base64
                var msb = new Base64DecoderInputStream(input, true);
                var iv = StreamUtils.ReadBytes(msb, optionsToUse.IVLength);
                var salt = StreamUtils.ReadBytes(msb, optionsToUse.SaltLength);
                var iterations4 = StreamUtils.ReadBytes(msb, 4);
                EnsureLength(iv, optionsToUse.IVLength, "IV");
                EnsureLength(salt, optionsToUse.SaltLength, "salt");
                EnsureLength(iterations4, 4, "iteration metadata");
                var iterations = BitConverter.ToInt32(iterations4, 0);
                ValidateIterations(iterations);
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
                //var kkk = StreamUtils.ReadBytes(msb);
                var cryptoStream = new CryptoStream(new LeaveOpenInputStream(msb), decryptor, CryptoStreamMode.Read);
                //skip the encrypted Base64 salt marker
                var saltMarkerLength = checked(4 * ((optionsToUse.SaltLength + 2) / 3));
                var saltMarker = StreamUtils.ReadBytes(cryptoStream, saltMarkerLength);
                EnsureLength(saltMarker, saltMarkerLength, "salt marker");
                //return
                return cryptoStream;
            } else if (optionsToUse.Encoding == Encoding.Binary) {
                //binary
                var iv = StreamUtils.ReadBytes(input, optionsToUse.IVLength);
                var salt = StreamUtils.ReadBytes(input, optionsToUse.SaltLength);
                var iterations4 = StreamUtils.ReadBytes(input, 4);
                EnsureLength(iv, optionsToUse.IVLength, "IV");
                EnsureLength(salt, optionsToUse.SaltLength, "salt");
                EnsureLength(iterations4, 4, "iteration metadata");
                var iterations = BitConverter.ToInt32(iterations4, 0);
                ValidateIterations(iterations);
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
                //skip the encrypted binary salt marker
                var saltMarker = StreamUtils.ReadBytes(cryptoStream, optionsToUse.SaltLength);
                EnsureLength(saltMarker, optionsToUse.SaltLength, "salt marker");
                //return
                return cryptoStream;
            } else {
                throw new FormatException("Unable to decrypt: invalid encoding.");
            }

        }


        // methods (private)
        private static void ValidateOptions(Options options) {
            if (options.SaltLength < 0 || options.SaltLength > 1024 * 1024) throw new FormatException("Unable to decrypt: invalid salt length.");
            if (options.IVLength < 0 || options.IVLength > 1024) throw new FormatException("Unable to decrypt: invalid IV length.");
            if (options.KeySize <= 0 || options.KeySize > 1024) throw new FormatException("Unable to decrypt: invalid key size.");
            if (options.BlockSize <= 0 || options.BlockSize > 1024) throw new FormatException("Unable to decrypt: invalid block size.");
            if (!Enum.IsDefined(typeof(Encoding), options.Encoding)) throw new FormatException("Unable to decrypt: invalid encoding.");
            if (!Enum.IsDefined(typeof(CipherMode), options.CipherMode)) throw new FormatException("Unable to decrypt: invalid cipher mode.");
            if (!Enum.IsDefined(typeof(PaddingMode), options.PaddingMode)) throw new FormatException("Unable to decrypt: invalid padding mode.");
        }
        private static void ValidateIterations(int iterations) {
            if (iterations <= 0 || iterations > 10000000) throw new FormatException("Unable to decrypt: invalid iteration count.");
        }
        private static void EnsureLength(byte[] value, int expectedLength, string name) {
            if (value.Length != expectedLength) throw new FormatException("Unable to decrypt: incomplete " + name + ".");
        }

    }


}
