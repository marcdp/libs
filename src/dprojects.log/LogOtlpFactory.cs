using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Log {

    [Protocol("otlp", "")]
    [ProtocolUsage("otlp:")]
    public class LogOtlpFactory() : IFactoryByUrl<ILog> {

        public ILog Create(string src) {  
            var url = new System.Uri(src);
            var service = UrlUtils.GetQueryValue(url.Query, "service", "");
            var scope = UrlUtils.GetQueryValue(url.Query, "scope", "");
            return new LogOtlp(url.Host, url.Port, service, scope);
        }
    } 

}
  

