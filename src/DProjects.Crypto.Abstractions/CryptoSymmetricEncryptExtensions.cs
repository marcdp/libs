
using DProjects.Utils;
using System.IO;
using System.Threading.Tasks;

namespace DProjects.Crypto {


    public static class CryptoSymmetricEncryptExtensions {


        //methods
        public static void Encrypt(this ICryptoSymmetricEncrypt cryptoSimmetricEncrypt, Stream input, Stream output, string password) {
            using (var cryptoStream = cryptoSimmetricEncrypt.GetStream(output, password)) {
                StreamUtils.Copy(input, cryptoStream);
            }
        }
        public static async Task EncryptAsync(this ICryptoSymmetricEncrypt cryptoSimmetricEncrypt, Stream input, Stream output, string password) {
            using (var cryptoStream = cryptoSimmetricEncrypt.GetStream(output, password)) {
                await StreamUtils.CopyAsync(input, cryptoStream);
            }
        }
        public static string Encrypt(this ICryptoSymmetricEncrypt cryptoSimmetricEncrypt, string input, string password) {
            var encoding = System.Text.Encoding.UTF8;
            using (var outputStream = new MemoryStream()) {
                using (var inputStream = new MemoryStream(encoding.GetBytes(input))) {
                    cryptoSimmetricEncrypt.Encrypt(inputStream, outputStream, password);
                }
                return encoding.GetString(outputStream.ToArray());
            }
        }
        public static byte[] Encrypt(this ICryptoSymmetricEncrypt cryptoSimmetricEncrypt, byte[] input, string password) {
            using (var outputStream = new MemoryStream()) {
                using (var inputStream = new MemoryStream(input)) {
                    cryptoSimmetricEncrypt.Encrypt(inputStream, outputStream, password);
                }
                return outputStream.ToArray();
            }
        } 

    }

}