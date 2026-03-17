using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("stdout", "")]
    [ProtocolUsage("stdout:")]
    [ProtocolExample("stdout:?format=json&level=debug", "")]
    public class LogStdoutFactory(IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            var logLevel = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            return new LogStdout(logFormatter, logLevel);
        }

    }

}


