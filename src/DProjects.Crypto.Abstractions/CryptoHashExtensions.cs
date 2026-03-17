
using DProjects.Utils;
using System.IO;

namespace DProjects.Crypto {


    public static class CryptoHashExtensions {


        //methods
        public static byte[] ToHash(this ICryptoHash cryptoHash, string input) {
            return cryptoHash.ToHash(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input)));
        }

    }

}