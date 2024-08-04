using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

namespace DProjects.Secrets {

    public class SecretManagerUserSecrets : ISecretManager {

        
        //fields
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private IDictionary<string, string>? mItems;


        //constructor
        public SecretManagerUserSecrets(IFilesystem filesystem, string path) {
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
        public Task Seal(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public Task Seal(string password, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        //methods
        public async Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken) {
            var result = new List<Secret>();
            var items = await Load(cancellationToken);
            foreach(var key in items.Keys) {
                if (string.IsNullOrEmpty(pattern) || StringUtils.Like(key, pattern)) {
                    var secret = new Secret(key, "", items[key]);
                    result.Add(secret);
                }
            }
            return result.ToArray();
        }
        public async Task SetAsync(Secret secret, CancellationToken cancellationToken) {
            var items = await Load(cancellationToken);
            items[secret.Name] = secret.GetValue();
            await Save(cancellationToken);
        }
        public async Task<bool> DelAsync(string path, CancellationToken cancellationToken) {
            var items = await Load(cancellationToken);
            if (!items.ContainsKey(path)) return false;
            items.Remove(path);
            await Save(cancellationToken);
            return true;
        }
        public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var items = await Load(cancellationToken);
            if (items.TryGetValue(name, out var value)) {
                return new Secret(name, "", value);
            }
            return null;
        }

        //private methods
        private async Task<IDictionary<string, string>> Load(CancellationToken cancellationToken) {
            if (mItems == null) {
                var json = await mFilesystem.LoadTextFileAsync(mPath);
                if (json == "") {
                    json = "{}";
                    await mFilesystem.SaveTextFileAsync(mPath, json, System.Text.Encoding.UTF8, cancellationToken);
                }
                mItems = JsonSerializer.Deserialize<IDictionary<string,string>>(json);
            }
            return mItems!;
        }
        private async Task Save(CancellationToken cancellationToken) {
            if (mItems != null) {
                var json = JsonSerializer.Serialize(mItems, new JsonSerializerOptions() {
                     WriteIndented = true
                });
                await mFilesystem.SaveTextFileAsync(mPath, json, System.Text.Encoding.UTF8, cancellationToken);
            }
        }

    }

}