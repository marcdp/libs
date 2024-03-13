using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.IO;

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

        //methods
        public byte[] ToHash(Stream data) {
            var text = System.Text.Encoding.UTF8.GetString(StreamUtils.ReadBytes(data));  
            var result = BCrypt.Net.BCrypt.HashPassword(text);
            return System.Text.Encoding.UTF8.GetBytes(result);
        } 
        public string ToHash(string text) {
            return BCrypt.Net.BCrypt.HashPassword(text); 
        }
        public string ToHash(string text, string salt) {
            return BCrypt.Net.BCrypt.HashPassword(text, salt);
        }
        public bool Verify(string text, string hash) {
            return BCrypt.Net.BCrypt.Verify(text, hash);
        }
        public bool Verify(Stream data, byte[] hash) {
            var text = System.Text.Encoding.UTF8.GetString(StreamUtils.ReadBytes(data));
            return BCrypt.Net.BCrypt.Verify(text, System.Text.Encoding.UTF8.GetString(hash));
        }

    }


}
