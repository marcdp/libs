using DProjects.Factories.Attributes;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;

namespace DProjects.Crypto {


    [Protocol("pbkdf2", "")]
    [ProtocolExample("pbkdf2://?[iterations=XX][&keyLength=32]","")]
    public class CryptoKeyDerivationPBKDF2 : ICryptoKeyDerivation {


        //options
        public class Options {
            public int Iterations { get; set; } = 1000;
            public int KeyLength { get; set; } = 32;
            public KeyDerivationPrf Prf { get; set; } = KeyDerivationPrf.HMACSHA256;
        }


        //variables
        private Options mOptions;


        //constructor
        public CryptoKeyDerivationPBKDF2(Options options) {
            mOptions = options;
        }
        public void Dispose() { 
        }
        

        //methods
        public byte[] Derive(string password, byte[] salt) {
            return KeyDerivation.Pbkdf2(password, salt, mOptions.Prf, mOptions.Iterations, mOptions.KeyLength);
        }

    }

}
