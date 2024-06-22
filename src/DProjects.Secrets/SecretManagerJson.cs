using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
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
        private readonly object mLock = new();
        private Storage? mStorage;


        //constructor
        public SecretManagerJson(IFilesystem filesystem, string path) {
            mFilesystem = filesystem;
            mPath = string.IsNullOrEmpty(path) ? "/" : path;
        }

        //methods
        public Task<bool> IsSealed(CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }
        public Task<bool> Unseal(string pass, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }
        public Task Seal(string? password, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        //methods
        public async Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken) {
            var storage = await Load(cancellationToken);
            lock (mLock) {
                var result = new List<Secret>();
                foreach (var secret in storage.Secrets) {
                    if (string.IsNullOrEmpty(pattern) || StringUtils.Like(secret.Name, pattern)) {
                        result.Add(secret);
                    }
                }
                return result.ToArray();
            }
        }
        public async Task SetAsync(Secret secret, CancellationToken cancellationToken) {
            var storage = await Load(cancellationToken);
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
            await Save(cancellationToken);
        }
        public async Task<bool> DelAsync(string name, CancellationToken cancellationToken) {
            var storage = await Load(cancellationToken);
            lock (mLock) {
                var secret = storage.Secrets.Where(x => x.Name == name).FirstOrDefault();
                if (secret == null) return false;
                storage.Secrets.Remove(secret);
            }
            await Save(cancellationToken);
            return true;
        }
        public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var storage = await Load(cancellationToken);
            lock (mLock) {
                return storage.Secrets.Where(x => x.Name == name).FirstOrDefault();
            }
        }

        //private methods
        private async Task<Storage> Load(CancellationToken cancellationToken) {
            if (mStorage == null) {
                var json = await mFilesystem.LoadTextFileAsync(mPath);
                if (json == "") {
                    json = "{}";
                    await mFilesystem.SaveTextFileAsync(mPath, json, System.Text.Encoding.UTF8, cancellationToken);
                }
                mStorage = JsonSerializer.Deserialize<Storage>(json,  new JsonSerializerOptions() { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase                      
                });
                if (mStorage == null) throw new Exception("Unable to load secrets: unable to deserialize json: null value");
            }
            return mStorage;
        }
        private async Task Save(CancellationToken cancellationToken) {
            if (mStorage != null) {
                var json = JsonSerializer.Serialize(mStorage, new JsonSerializerOptions() {
                     WriteIndented = true,
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await mFilesystem.SaveTextFileAsync(mPath, json, System.Text.Encoding.UTF8, cancellationToken);
            }
        }

    }

}