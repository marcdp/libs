using DProjects.Factories.Attributes;
using DProjects.Streams;
using DProjects.Utils;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.IO;
using System.Security.Cryptography;

namespace DProjects.Crypto {


    [Protocol("aes", "")]
    [ProtocolUsage("")]
    [ProtocolExample("aes:?fold=76", "")]
    [ProtocolExample("aes:?encoding=binary", "")]
    [ProtocolExample("aes:?encoding=base64&cipher=ECB&ivLength=32", "")]
    public class CryptoSymmetricEncryptAES : ICryptoSymmetricEncrypt {

        //enums
        public enum Encoding {
            Base64,
            Binary
        }

        //options
        public class Options {
            public int IterationsMin { get; set; } = 50000;
            public int IterationsRandomRange { get; set; } = 50000;
            public int SaltLength { get; set; } = 16;
            public int IVLength { get; set; } = 16;
            public byte[]? IV { get; set; }
            public int BlockSize { get; set; } = 16;
            public PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;
            public CipherMode CipherMode { get; set; } = CipherMode.CBC;
            public int KeySize { get; set; } = 32;
            public Encoding Encoding { get; set; } = Encoding.Base64;
            public int Fold { get; set; } = 0;
            public string Version { get; set; } = "";
            public char Separator { get; set; } = ',';
            public bool Header { get; set; } = true;
        }


        //variables
        private Options mOptions;


        //constructor
        public CryptoSymmetricEncryptAES(Options options) {
            mOptions = options;
        }
        public void Dispose() {
        }


        //methods
        public Stream GetStream(Stream output, string password) {
            //prepare
            var salt = RandomUtils.GenerateSalt(mOptions.SaltLength);
            var iterations = (new Random()).Next(mOptions.IterationsMin, mOptions.IterationsMin + mOptions.IterationsRandomRange);
            var iterations4 = BitConverter.GetBytes(iterations);
            //derive key from password using Pbkdf2 algorithm with N iterations
            var key = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, mOptions.KeySize); 
            //prepare
            var iv = mOptions.IV ?? RandomUtils.GenerateSalt(mOptions.IVLength); //random iv
            var aes = System.Security.Cryptography.Aes.Create();
            aes.Mode = mOptions.CipherMode;
            aes.Padding = mOptions.PaddingMode;
            aes.BlockSize = mOptions.BlockSize * 8;
            aes.KeySize = mOptions.KeySize * 8;
            aes.Key = key;
            aes.IV = iv;
            //fold
            if (mOptions.Encoding == Encoding.Base64) {
                if (mOptions.Fold > 0) output = new FoldedOutputStream(output, mOptions.Fold, false);
            }
            //header
            if (mOptions.Header) {
                var header = UrlUtils.Serialize("aes", mOptions, new () {
                    Excluded = new string[] { "IterationsMin", "IterationsRandomRange", "Separator", "Fold", "IV" }
                });
                var headerBuffer = System.Text.Encoding.UTF8.GetBytes(header + mOptions.Separator);
                output.Write(headerBuffer, 0, headerBuffer.Length);
            }
            //encryptor
            var encryptor = aes.CreateEncryptor();
            //encoding
            if (mOptions.Encoding == Encoding.Base64) {
                //write iv + salt + iterations                
                var base64EncoderOutputStream = new Base64EncoderOutputStream(output, true);
                base64EncoderOutputStream.Write(iv, 0, iv.Length);
                base64EncoderOutputStream.Write(salt, 0, salt.Length); 
                base64EncoderOutputStream.Write(iterations4, 0, iterations4.Length);
                var cryptoStream = new CryptoStream(base64EncoderOutputStream, encryptor, CryptoStreamMode.Write);
                //write salt to the cripto stream in base64
                var saltB64Buffer = System.Text.Encoding.ASCII.GetBytes(Convert.ToBase64String(salt));
                cryptoStream.Write(saltB64Buffer, 0, saltB64Buffer.Length);
                //return
                return cryptoStream;
            } else if (mOptions.Encoding == Encoding.Binary) {
                //write iv + salt + iterations
                output.Write(iv, 0, iv.Length);
                output.Write(salt, 0, salt.Length);
                output.Write(iterations4, 0, iterations4.Length);
                //crypto
                var cryptoStream = new CryptoStream(new LeaveOpenOutputStream(output), encryptor, CryptoStreamMode.Write);
                //write salt to the cripto stream
                cryptoStream.Write(salt, 0, salt.Length);
                //return
                return cryptoStream;
            } else {
                throw new NotImplementedException();
            }
        }


    }

}
