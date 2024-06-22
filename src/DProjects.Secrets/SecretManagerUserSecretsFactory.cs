using System;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;


namespace DProjects.Secrets {

    [Protocol("user-secrets", "")]
    [ProtocolUsage("user-secrets:FILESYSTEM")]
    [ProtocolExample("user-secrets:file:///path/to/secrets.json", "")]
    [ProtocolExample("user-secrets:fs-file:/path/to/secrets.json", "")]
    public class SecretManagerUserSecretsFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<ISecretManager> {
        public ISecretManager Create(string src) {
            var url = new System.Uri(src);
            var (outerUrl, innerUrl) = UrlUtils.UnwrapUrl(src);
            var aOuterUrl = new Uri(outerUrl);
            var aInnerUrl = new Uri(innerUrl);
            var filesystem = fsFactory.Create(innerUrl);
            return new SecretManagerUserSecrets(filesystem, aOuterUrl.AbsolutePath);
        }

    }

}
