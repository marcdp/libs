using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Factories;
using DProjects.Fs;

namespace DProjects.Vault {

    public class VaultXml : IVault {


        //var


        //ctor
        public VaultXml(IFilesystem filesystem, string path, string password) {
        }
        public void Dispose() {
        }


        //add
        public Task AddAsync(VaultEntry entry, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        //get
        public Task<VaultEntry?> GetAsync(string path, string? version, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        public Task<VaultEntry[]> GetVersionsAsync(string path, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        public Task<byte[]> GetValueAsync(string path, string? version, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        //list
        public Task<VaultEntry[]> ListAsync(string path, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        //remove
        public Task RemoveAsync(string path, string? version, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        //rotate
        public Task RotateAsync(string path, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
    }

}