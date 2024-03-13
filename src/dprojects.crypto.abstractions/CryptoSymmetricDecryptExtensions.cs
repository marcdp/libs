
using DProjects.Utils;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace DProjects.Crypto {


    public static class CryptoSymmetricDecryptExtensions {


        //methods
        public static void Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, Stream input, Stream output, string password) {
            cryptoSimmetricDecrypt.Decrypt(input, output, (version) => password);
        }
        public static void Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, Stream input, Stream output, Func<string,string> passwordProvider) {
            using (var cryptoStream = cryptoSimmetricDecrypt.GetStream(input, passwordProvider)) {
                StreamUtils.Copy(cryptoStream, output);
            }
        }
        public static byte[] Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, byte[] input, string password) {
            return cryptoSimmetricDecrypt.Decrypt(input, (version) => password);
        }
        public static byte[] Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, byte[] input, Func<string, string> passwordProvider) {
            using (var outputStream = new MemoryStream()) {
                using (var inputStream = new MemoryStream(input)) {
                    cryptoSimmetricDecrypt.Decrypt(inputStream, outputStream, passwordProvider);
                }
                return outputStream.ToArray();
            }
        }
        public static string Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, string input, string password) {
            return cryptoSimmetricDecrypt.Decrypt(input, (version) => password);
        }
        public static string Decrypt(this ICryptoSymmetricDecrypt cryptoSimmetricDecrypt, string input, Func<string, string> passwordProvider) {
            var encoding = System.Text.Encoding.UTF8;
            using (var outputStream = new MemoryStream()) {
                using (var inputStream = new MemoryStream(encoding.GetBytes(input))) {
                    cryptoSimmetricDecrypt.Decrypt(inputStream, outputStream, passwordProvider);
                }
                return encoding.GetString(outputStream.ToArray());
            }
        }

    }

}