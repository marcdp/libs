using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using System.Collections.Generic;

namespace DProjects.Secrets {

    public class SecretProviderUserSecrets(IFilesystem filesystem, string path) : ISecretProvider {


        //methods
        public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var aux = await LoadJsonAsync(cancellationToken);
            if (aux.TryGetPropertyValue(name, out var value)) {
                if (value != null) {
                    return new Secret(name, "", value.GetValue<string>());
                }
            }
            return null;
        }
        public async Task<string[]> GetNamesAsync(CancellationToken cancellationToken) {
            var aux = await LoadJsonAsync(cancellationToken);
            var keys = new List<string>();
            foreach (var key in aux) {
                keys.Add(key.Key);
            }
            return keys.ToArray();
        }

        //private
        private async Task<JsonObject> LoadJsonAsync(CancellationToken cancellationToken) {
            var entry = await filesystem.GetEntryAsync(path, cancellationToken);
            if (entry == null) return null;
            var json = await filesystem.LoadTextFileAsync(path);
            return JsonNode.Parse(json)!.AsObject();
        }
    }

}