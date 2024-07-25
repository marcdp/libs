using System;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("json", "")]
    [ProtocolUsage("json:FILESYSTEM")]
    [ProtocolExample("json:file:///path/to/secrets.json.aes?init=true", "")]
    [ProtocolExample("json:file:///d!/secrets.json.aes?init=true", "")]
    [ProtocolExample("json:mem:!/secrets.json.aes?init=true", "")]
    
    public class SecretManagerJsonFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<ISecretManager> {
        public ISecretManager Create(string src) {
            var (outerUrl, innerUrl) = UrlUtils.UnwrapUrl(src);

            var aOuterUrl = new Uri(outerUrl);
            var aInnerUrl = new Uri(innerUrl);
            var init = UrlUtils.GetQueryValue<bool>(aInnerUrl.Query, "init");

            var filesystem = fsFactory.Create(innerUrl);
            return new SecretManagerJson(filesystem, aOuterUrl.AbsolutePath, init);
        }
    }







}
