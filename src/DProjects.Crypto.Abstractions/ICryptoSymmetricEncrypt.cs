using System;
using System.IO;

namespace DProjects.Crypto {

    public interface ICryptoSymmetricEncrypt : IDisposable {

        public Stream GetStream(Stream output, string password);
        
    }


}
