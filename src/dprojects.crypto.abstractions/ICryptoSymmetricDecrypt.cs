using System;
using System.IO;

namespace DProjects.Crypto {

    public interface ICryptoSymmetricDecrypt : IDisposable {

        public Stream GetStream(Stream input, string password);
        public Stream GetStream(Stream input, Func<string,string> passwordProvider);

    }


}
