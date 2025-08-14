using System;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log;
using DProjects.Log.Serializers;
using DProjects.Utils;

namespace DProjects.Log.Serializers {

    [Protocol("otlp", "")]
    [ProtocolExample("otlp:", "")]
    public class LogEntrySerializerOtlpFactory : IFactoryByUrl<ILogEntrySerializer> {

        public ILogEntrySerializer Create(string src) {
            var url = new Uri(src);
            var service = UrlUtils.GetQueryValue(url.Query, "service", "");
            var scope = UrlUtils.GetQueryValue(url.Query, "scope", "");
            return new LogEntrySerializerOtlp(service, scope);
        }
    }

}
