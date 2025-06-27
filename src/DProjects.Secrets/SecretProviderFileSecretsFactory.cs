
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("file:", "")]
    public class SecretProviderFileFactory : IFactoryByUrl<ISecretProvider> {
        public ISecretProvider Create(string src) {
            var url = new System.Uri(src);
            return new SecretProviderFileSecrets(url.AbsolutePath);
        }

    }

}
