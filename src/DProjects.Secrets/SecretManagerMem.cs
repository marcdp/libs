using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

namespace DProjects.Secrets {

    public class SecretManagerMem(string password) : ISecretManager {

        
        //vars
        private string mPassword = password;
        private bool mSealed = true;
        private ConcurrentDictionary<string, Secret> mSecrets = new();


        //methods
        public Task<bool> IsSealed(CancellationToken cancellationToken) {
            return Task.FromResult(mSealed);
        }
        public Task<bool> Unseal(string pass, CancellationToken cancellationToken) {
            if (mPassword.Equals(pass, StringComparison.Ordinal)) {
                mSealed = false;
            }
            return Task.FromResult(!mSealed);
        }
        public Task Seal(string? password, CancellationToken cancellationToken) {
            mPassword = password ?? mPassword;
            mSealed = true;
            return Task.CompletedTask;
        }


        //methods
        public Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken) {
            if (mSealed) throw new Exception("Unable to list secrets: secrets unsealed");
            var result = new List<Secret>();
            foreach (var secret in mSecrets.Values) {
                if (string.IsNullOrEmpty(pattern) || StringUtils.Like(secret.Name, pattern)) {
                    result.Add(secret);
                }
            }
            return Task.FromResult(result.ToArray());
        }
        public Task SetAsync(Secret secret, CancellationToken cancellationToken) {
            if (mSealed) throw new Exception("Unable to set secret: secrets unsealed");
            mSecrets.AddOrUpdate(secret.Name, secret, (key, oldValue) => secret);
            return Task.CompletedTask;
        }
        public Task<bool> DelAsync(string path, CancellationToken cancellationToken) {
            if (mSealed) throw new Exception("Unable to delete secret: secrets unsealed");
            if (mSecrets.TryRemove(path, out var secret)) {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            if (mSealed) throw new Exception("Unable to get secret: secrets unsealed");
            if (mSecrets.TryGetValue(name, out var secret)) {
                return Task.FromResult<Secret?>(secret);
            }
            return Task.FromResult<Secret?>(null);
        }

    }

}