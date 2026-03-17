using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("fs-file", "")]
    [ProtocolUsage("fs-file://PATH-TO-FILE")]
    [ProtocolExample("fs-file:///var/log/file.log?autoFlush=true&useWriterThread=true&logFormatter=rat&level=debug", "")]
    public class LogFsFileFactory(IFilesystem filesystem, IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var truncate = UrlUtils.GetQueryValue(url.Query, "truncate", false);
            var autoFlush = UrlUtils.GetQueryValue(url.Query, "autoFlush", false);
            var useWriterThread = UrlUtils.GetQueryValue(url.Query, "useWriterThread", true);
            var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            var level = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            return new LogFsFile(filesystem, url.AbsolutePath, truncate, autoFlush, useWriterThread, logFormatter, level);
        }

    }

}


 