using System;
using System.IO;

namespace DProjects.Crypto {

    public interface ICryptoKeyDerivation : IDisposable {

        public byte[] Derive(string password, byte[] salt);

    }


}
