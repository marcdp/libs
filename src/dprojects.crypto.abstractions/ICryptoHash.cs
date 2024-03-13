using System;
using System.IO;

namespace DProjects.Crypto {

    public interface ICryptoHash : IDisposable {

        byte[] ToHash(Stream data);
        bool Verify(Stream data, byte[] hash);

    }


}
