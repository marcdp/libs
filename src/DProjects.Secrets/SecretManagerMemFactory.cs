
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;


namespace DProjects.Secrets {

    [Protocol("mem", "")]
    [ProtocolUsage("mem:?password=PASSWORD")]
    [ProtocolExample("mem:?password=123456", "")]
    public class SecretManagerMemFactory() : IFactoryByUrl<ISecretManager> {
        public ISecretManager Create(string src) {
            var url = new System.Uri(src);
            var password = UrlUtils.GetQueryValue<string>(url.Query, "password", "");
            return new SecretManagerMem(password);
        }

    }

}
