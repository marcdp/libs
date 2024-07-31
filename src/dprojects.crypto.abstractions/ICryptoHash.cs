using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Crypto {

    public interface ICryptoHash : IDisposable {

        byte[] ToHash(Stream data);
        Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken);
        bool Verify(Stream data, byte[] hash);
        Task<bool> VerifyAsync(Stream data, byte[] hash, CancellationToken cancellationToken);

    }


}
