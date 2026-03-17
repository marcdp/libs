using DProjects.Factories.Attributes;
using DProjects.Utils;

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Crypto {


    [Protocol("sha512", "")]
    public class CryptoHashSHA512 : ICryptoHash {


        //options
        public class Options {
        }

        //constructor
        public CryptoHashSHA512(Options options) {
        }
        public void Dispose() {
        }

        //props
        public string Url => "sha512:";


        //methods
        public byte[] ToHash(Stream data) {
            using (var algorithm = SHA512.Create()) {
                return algorithm.ComputeHash(data);
            }
        }
        public string ToHashText(string input) {
            return HexUtils.Hex(ToHash(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input))));
        }
        public Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken) {
            using (var algorithm = SHA512.Create()) {
                return Task.FromResult(algorithm.ComputeHash(data));
            }
        }

        //verify
        public bool Verify(Stream data, byte[] hash) {
            var hashComputed = ToHash(data);
            return hashComputed.SequenceEqual<byte>(hash);
        }
        public bool VerifyText(string text, string hash) {
            return ToHashText(text).Equals(hash);
        }
        public Task<bool> VerifyAsync(Stream data, byte[] hash, CancellationToken cancellationToken) {
            var hashComputed = ToHash(data);
            return Task.FromResult(hashComputed.SequenceEqual<byte>(hash));
        }


    }
}
