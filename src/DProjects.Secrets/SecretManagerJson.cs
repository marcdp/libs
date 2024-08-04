using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Crypto;
using DProjects.Utils;
using System.Threading;

namespace DProjects.Secrets {

    public class SecretManagerJson : ISecretManager {


        //class
        private class Storage { 
            public List<Secret> Secrets { get; set; } = new();
        }


        //fields
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private bool mInit;
        private readonly object mLock = new();
        private string? mPassword;
        private Storage? mStorage;


        //constructor
        public SecretManagerJson(IFilesystem filesystem, string path, bool init) {
            mFilesystem = filesystem;
            mPath = string.IsNullOrEmpty(path) ? "/" : path;
            mInit = init;
        }


        //methods
        public Task<bool> IsSealed(CancellationToken cancellationToken) {
            return Task.FromResult(mStorage == null);
        }
        public async Task<bool> Unseal(string password, CancellationToken cancellationToken) {
            mStorage = await Load(password, cancellationToken);
            return mStorage != null;
        }
        public Task Seal(CancellationToken cancellationToken) {
            mStorage = null;
            return Task.CompletedTask;
        }
        public async Task Seal(string password, CancellationToken cancellationToken) {
            await Save(password, cancellationToken);
            mStorage = null;
        }


        //methods
        public Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken) {
            var storage = mStorage ?? throw new Exception("Unablet to list secrets: secrets sealed");
            lock (mLock) {
                var result = new List<Secret>();
                foreach (var secret in storage.Secrets) {
                    if (string.IsNullOrEmpty(pattern) || StringUtils.Like(secret.Name, pattern)) {
                        result.Add(secret);
                    }
                }
                return Task.FromResult(result.ToArray());
            }
        }
        public async Task SetAsync(Secret secret, CancellationToken cancellationToken) {
            var storage = mStorage ?? throw new Exception("Unablet to set secret: secrets sealed");
            lock (mLock) {
                var index = storage.Secrets.IndexOf(secret);
                if (index == -1) {
                    storage.Secrets.Add(secret);
                    storage.Secrets.Sort((a, b) => {
                        return a.Name.CompareTo(b.Name);
                    });
                } else {
                    storage.Secrets[index] = secret;
                }
            }
            if (mPassword != null) {
                await Save(mPassword, cancellationToken);
            }
        }
        public async Task<bool> DelAsync(string name, CancellationToken cancellationToken) {
            var storage = mStorage ?? throw new Exception("Unablet to delete secret: secrets sealed");
            lock (mLock) {
                var secret = storage.Secrets.Where(x => x.Name == name).FirstOrDefault();
                if (secret == null) return false;
                storage.Secrets.Remove(secret);
            }
            if (mPassword != null) {
                await Save(mPassword, cancellationToken);
            }
            return true;
        }
        public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var storage = mStorage ?? throw new Exception("Unablet to get secret: secrets sealed");
            lock (mLock) {
                return Task.FromResult<Secret?>(storage.Secrets.Where(x => x.Name == name).FirstOrDefault());
            }
        }


        //private methods
        private async Task<Storage> Load(string password, CancellationToken cancellationToken) {
            if (mStorage == null) {
                //create file if not exists
                if (mInit && !await mFilesystem.ExistsAsync(mPath, cancellationToken)) {
                    mStorage = new Storage();
                    await Save(password, cancellationToken);
                }
                //read file
                var aes = await mFilesystem.LoadTextFileAsync(mPath);
                var json = "";
                if (aes == "") {
                    //create file if empty
                    json = "{}";
                    using (var crypto = new DProjects.Crypto.CryptoSymmetricEncryptAES(new())) {
                        await mFilesystem.SaveTextFileAsync(mPath, crypto.Encrypt(json, password), System.Text.Encoding.UTF8, cancellationToken);
                    }
                } else {
                    //decrypt
                    using (var crypto = new DProjects.Crypto.CryptoSymmetricDecryptAES()) {
                        json = crypto.Decrypt(aes, password);
                    }
                }
                //deserialize
                mStorage = JsonSerializer.Deserialize<Storage>(json,  new JsonSerializerOptions() { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase                      
                });
                if (mStorage == null) throw new Exception("Unable to load secrets: unable to deserialize json: null value");
                //remember used password
                mPassword = password;
            }
            return mStorage;
        }
        private async Task Save(string password, CancellationToken cancellationToken) {
            if (mStorage != null) {
                //serialize
                var json = JsonSerializer.Serialize(mStorage, new JsonSerializerOptions() {
                     WriteIndented = true,
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                //encrypt
                var aes = "";
                var options = new DProjects.Crypto.CryptoSymmetricEncryptAES.Options() {
                    Fold = 76 
                };
                using (var crypto = new DProjects.Crypto.CryptoSymmetricEncryptAES(options)) {
                    aes = crypto.Encrypt(json, password);
                }
                //save
                await mFilesystem.SaveTextFileAsync(mPath, aes, System.Text.Encoding.UTF8, cancellationToken);
            }
        }

    }

}