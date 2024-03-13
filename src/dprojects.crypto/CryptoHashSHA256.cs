using DProjects.Factories.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace DProjects.Crypto {


    [Protocol("sha256", "")]
    public class CryptoHashSHA256 : ICryptoHash {


        //options
        public class Options {
        }

        //constructor
        public CryptoHashSHA256(Options options) {
        }
        public void Dispose() { 
        }


        //methods
        public byte[] ToHash(Stream data) {
            using (var algorithm = SHA256.Create()) {
                return algorithm.ComputeHash(data);
            }
        }
        public bool Verify(Stream data, byte[] hash) {
            var hashComputed = ToHash(data);
            return hashComputed.SequenceEqual<byte>(hash);
        }
    }


}
