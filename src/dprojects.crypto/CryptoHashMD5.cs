using DProjects.Factories.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Crypto {



    [Protocol("md5", "")]
    public class CryptoHashMD5 : ICryptoHash {


        //options
        public class Options {
        }


        //constructor
        public CryptoHashMD5(Options options) {
        }
        public void Dispose() { 
        }


        //methods
        public byte[] ToHash(Stream data) {
            using (var algorithm = MD5.Create()) {
                return algorithm.ComputeHash(data);
            }
        }
        public Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken) {
            using (var algorithm = MD5.Create()) {
                return Task.FromResult(algorithm.ComputeHash(data));
            }
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
