using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using System.Collections.Generic;
using DProjects.Utils;

namespace DProjects.Secrets {

    public class SecretProviderFileSecrets(string path) : ISecretProvider {


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
            var json = FileUtils.ReadTextFile(path);
            return JsonNode.Parse(json)!.AsObject();
        }
        private async Task<JsonObject?> LoadJsonAsync(CancellationToken cancellationToken) {
            var json = await FileUtils.ReadTextFileAsync(path);
            return JsonNode.Parse(json)!.AsObject();
        }
    }

}