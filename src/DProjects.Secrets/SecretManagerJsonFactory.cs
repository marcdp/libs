using System;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("json", "")]
    [ProtocolUsage("json:FILESYSTEM")]
    [ProtocolExample("json:file:///path/to/secrets.json", "")]
    [ProtocolExample("json:fs-file:/path/to/secrets.json", "")]
    public class SecretManagerJsonFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<ISecretManager> {
        public ISecretManager Create(string src) {
            var url = new System.Uri(src);
            var (outerUrl, innerUrl) = UrlUtils.UnwrapUrl(src);
            var aOuterUrl = new Uri(outerUrl);
            var aInnerUrl = new Uri(innerUrl);
            var filesystem = fsFactory.Create(innerUrl);
            return new SecretManagerJson(filesystem, aOuterUrl.AbsolutePath);
        }

    }

}
