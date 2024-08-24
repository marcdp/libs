using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Crypto {

    public interface ICryptoHash : IDisposable {

        //props
        string Url { get; }

        //to hash
        byte[] ToHash(Stream data);
        string ToHashText(string input);
        Task<byte[]> ToHashAsync(Stream data, CancellationToken cancellationToken);

        //verify
        bool Verify(Stream data, byte[] hash);
        bool VerifyText(string text, string hash);
        Task<bool> VerifyAsync(Stream data, byte[] hash, CancellationToken cancellationToken);
        

    }


}
