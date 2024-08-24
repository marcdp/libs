using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Crypto {

    [Protocol("bcrypt", "")]
    public class CryptoHashBCRYPT : ICryptoHash {

        //options
        public class Options {
        }


        //constructor
        public CryptoHashBCRYPT(Options? options = null) {
        }
        public void Dispose() { 
        }

        //props
        public string Url => "bcrypt:";

        //methods
        public byte[] ToHash(Stream data) {
            var text = System.Text.Encoding.UTF8.GetString(StreamUtils.ReadBytes(data));  
            var result = BCrypt.Net.BCrypt.HashPassword(text);
            return System.Text.Encoding.UTF8.GetBytes(result);
        }
        public string ToHash(string text) {
            return BCrypt.Net.BCrypt.HashPassword(text); 
        }
        public string ToHashText(string input) {
            return BCrypt.Net.BCrypt.HashPassword(input);
        }
        public string ToHash(string text, string salt) {
            return BCrypt.Net.BCrypt.HashPassword(text, salt);
        }
        public async Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken) {
            var text = System.Text.Encoding.UTF8.GetString(await StreamUtils.ReadBytesAsync(data, cancellationToken: cancellationToken));
            var result = BCrypt.Net.BCrypt.HashPassword(text);
            return System.Text.Encoding.UTF8.GetBytes(result);
        }

        //methosd verify
        public bool VerifyText(string text, string hash) {
            return BCrypt.Net.BCrypt.Verify(text, hash);
        }
        public bool Verify(Stream data, byte[] hash) {
            var text = System.Text.Encoding.UTF8.GetString(StreamUtils.ReadBytes(data));
            var hashAsText = System.Text.Encoding.UTF8.GetString(hash);
            return BCrypt.Net.BCrypt.Verify(text, hashAsText);
        }
        public async Task<bool> VerifyAsync(Stream data, byte[] hash, CancellationToken cancellationToken) {
            var text = System.Text.Encoding.UTF8.GetString(await StreamUtils.ReadBytesAsync(data, cancellationToken: cancellationToken));
            return BCrypt.Net.BCrypt.Verify(text, System.Text.Encoding.UTF8.GetString(hash));
        }


    }


}
