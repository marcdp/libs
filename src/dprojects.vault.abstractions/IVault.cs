using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Vault {

    public interface IVault : IDisposable {

        //add
        Task AddAsync(VaultEntry entry, CancellationToken cancellationToken);
        //get
        Task<VaultEntry?> GetAsync(string path, string? version, CancellationToken cancellationToken);
        Task<VaultEntry[]> GetVersionsAsync(string path, CancellationToken cancellationToken);
        Task<byte[]> GetValueAsync(string path, string? version, CancellationToken cancellationToken);
        //list
        Task<VaultEntry[]> ListAsync(string path, CancellationToken cancellationToken);
        //remove
        Task RemoveAsync(string path, string? version, CancellationToken cancellationToken);
        //rotate
        Task RotateAsync(string path, CancellationToken cancellationToken);

    }

}