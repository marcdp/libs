using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

namespace DProjects.Secrets {

    public class SecretProviderUserSecrets(IFilesystem filesystem, string path) : ISecretProvider {


        //methods
        public async Task<Secret?> GetAsync(string name) {
            var entry = await filesystem.GetEntryAsync(path);
            if (entry == null) return null;
            var json = await filesystem.LoadTextFileAsync(path);
            var aux = JsonNode.Parse(json)!.AsObject();
            if (aux.TryGetPropertyValue(name, out var value)) {
                if (value != null) {
                    return new Secret(name, value.ToString());
                }
            }
            return null;
        }

    }

}