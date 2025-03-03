
using System.Threading;
using DProjects.Utils;
using Microsoft.Extensions.Configuration;


namespace DProjects.Secrets {

    public class SecretsConfigurationProvider(string url) : Microsoft.Extensions.Configuration.ConfigurationProvider {
        public override void Load() {
            //load secrets from url like "dprojectstools:my_db" or "user-secrets:my-id"
            ISecretProvider? secretProvider = null;
            if (url.Equals("")) {
                return;
            } else if (url.StartsWith("dprojectstools:")) {
                secretProvider = new SecretProviderDProjectsToolsFactory().Create(url);
            } else {
                throw new System.Exception($"Unknown secret provider: {url}");
            }
            AsyncUtils.RunSync(async () => {
                foreach (var name in await secretProvider.GetNamesAsync(CancellationToken.None)) {
                    var value = await secretProvider.GetAsync(name, CancellationToken.None);
                    if (value != null) {
                        this.Data["Secrets:" + name] = value.GetValue();
                    }
                }
            });
        }
    }
    public class SecretsConfigurationSource(string url) : IConfigurationSource {
        public IConfigurationProvider Build(IConfigurationBuilder builder) {
            return new SecretsConfigurationProvider(url);
        }
    }
    public static class SecretsConfigurationExtensions {
        public static IConfigurationBuilder AddSecrets(this IConfigurationBuilder builder, string url) {
            return builder.Add(new SecretsConfigurationSource(url));
        }
    }


}
