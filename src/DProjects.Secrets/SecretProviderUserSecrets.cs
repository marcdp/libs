using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;

namespace DProjects.Secrets {

    public class SecretProviderUserSecrets(IFilesystem filesystem, string path) : ISecretProvider {


        //methods
        public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var entry = await filesystem.GetEntryAsync(path, cancellationToken);
            if (entry == null) return null;
            var json = await filesystem.LoadTextFileAsync(path);
            var aux = JsonNode.Parse(json)!.AsObject();
            if (aux.TryGetPropertyValue(name, out var value)) {
                if (value != null) {
                    return new Secret(name, "", value.GetValue<string>());
                }
            }
            return null;
        }

    }

}