using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Log.OpenTelemetry {

    [Protocol("otlp", "")]
    [ProtocolUsage("otlp:")]
    public class LogOtlpFactory() : IFactoryByUrl<ILog> {

        public ILog Create(string src) {  
            var url = new System.Uri(src);
            return new LogOtlp(url.Host, url.Port);
        }

    }

}
  

