using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using System.Collections.Generic;

namespace DProjects.Secrets {

    public class SecretProviderUserSecrets(IFilesystem filesystem, string path) : ISecretProvider {



        //methods
        public Secret? Get(string name) {
            var aux = LoadJson();
            if (aux != null && aux.TryGetPropertyValue(name, out var value)) {
                if (value != null) {
                    return new Secret(name, "", value.GetValue<string>());
                }
            }
            return null;
        }
        public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var aux = await LoadJsonAsync(cancellationToken);
            if (aux != null && aux.TryGetPropertyValue(name, out var value)) {
                if (value != null) {
                    return new Secret(name, "", value.GetValue<string>());
                }
            }
            return null;
        }

        //private
        private JsonObject? LoadJson() {
            var entry = filesystem.GetEntry(path);
            if (entry == null) return null;
            var json = filesystem.LoadTextFile(path);
            return JsonNode.Parse(json)!.AsObject();
        }
        private async Task<JsonObject?> LoadJsonAsync(CancellationToken cancellationToken) {
            var entry = await filesystem.GetEntryAsync(path, cancellationToken);
            if (entry == null) return null;
            var json = await filesystem.LoadTextFileAsync(path);
            return JsonNode.Parse(json)!.AsObject();
        }
    }

}