
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("dprojectstools", "")]
    [ProtocolUsage("dprojectstools:NAME")]
    [ProtocolExample("dprojectstools:MY_APP", "")]
    public class SecretProviderDProjectsToolsFactory : IFactoryByUrl<ISecretProvider> {
        public ISecretProvider Create(string src) {
            var url = new System.Uri(src);
            return new SecretProviderDProjectsTools(url.AbsolutePath);
        }

    }

}
