using DProjects.Factories.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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


        //methods
        public byte[] ToHash(Stream data) {
            using (var algorithm = SHA1.Create()) {
                return algorithm.ComputeHash(data);
            }
        }
        public bool Verify(Stream data, byte[] hash) {
            var hashComputed = ToHash(data);
            return hashComputed.SequenceEqual<byte>(hash);
        }

    }


}
