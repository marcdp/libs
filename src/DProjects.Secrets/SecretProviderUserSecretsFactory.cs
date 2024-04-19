
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("user-secrets", "")]
    public class SecretProviderUserSecretsFactory(IFilesystem filesystem) : IFactoryByUrl<ISecretProvider> {
        public ISecretProvider Create(string src) {
            var url = new System.Uri(src);
            return new SecretProviderUserSecrets(filesystem, url.AbsolutePath);
        }

    }

}
