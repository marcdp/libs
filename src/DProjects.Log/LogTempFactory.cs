using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("temp", "")]
    [ProtocolUsage("temp:")]
    [ProtocolExample("temp:?autoFlush=true&useWriterThread=true&logFormatter=rat&level=debug", "")]
    public class LogTempFactory(IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var autoFlush = UrlUtils.GetQueryValue(url.Query, "autoFlush", false);
            var useWriterThread = UrlUtils.GetQueryValue(url.Query, "useWriterThread", true);
            var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            var level = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            return new LogTemp(null, autoFlush, useWriterThread, logFormatter, level);
        }

    }

}


