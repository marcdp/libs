using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;

namespace DProjects.Log.OpenTelemetry {

    [Protocol("otlp", "OpenTelemetry Protocol logging")]
    [ProtocolUsage("otlp://host:port?service=service-name&scope=scope-name")]
    public class LogOtlpFactory() : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            if (!url.Scheme.Equals("otlp", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("OTLP log URLs must use the 'otlp' scheme.", nameof(src));
            }
            var service = UrlUtils.GetQueryValue(url.Query, "service", "");
            var scope = UrlUtils.GetQueryValue(url.Query, "scope", "");
            var port = url.IsDefaultPort ? 4318 : url.Port;
            return new LogOtlp(url.Host, port, service, scope);
        }

    }

}
