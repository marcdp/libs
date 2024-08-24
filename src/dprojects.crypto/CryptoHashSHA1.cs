using DProjects.Factories.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Utils;

namespace DProjects.Crypto {


    [Protocol("sha1", "")]
    public class CryptoHashSHA1 : ICryptoHash {


        //options
        public class Options {
        }

        //constructor
        public CryptoHashSHA1(Options options) {
        }
        public void Dispose() { 
        }

        //props
        public string Url => "sha1:";

        //tohash
        public byte[] ToHash(Stream data) {
            using (var algorithm = SHA1.Create()) {
                return algorithm.ComputeHash(data);
            }
        }
        public string ToHashText(string input) {
            return HexUtils.Hex(ToHash(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input))));
        }
        public Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken) {
            using (var algorithm = SHA1.Create()) {
                return Task.FromResult(algorithm.ComputeHash(data));
            }
        }

        //verify
        public bool VerifyText(string text, string hash) {
            return ToHashText(text).Equals(hash);
        }
        public bool Verify(Stream data, byte[] hash) {
            var hashComputed = ToHash(data);
            return hashComputed.SequenceEqual<byte>(hash);
        }
        public Task<bool> VerifyAsync(Stream data, byte[] hash, CancellationToken cancellationToken) {
            var hashComputed = ToHash(data);
            return Task.FromResult(hashComputed.SequenceEqual<byte>(hash));
        }

    }


}
