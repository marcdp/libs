using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("fs-dir", "")]
    [ProtocolUsage("fs-dir://PATH-TO-FILE?format=FORMAT")]
    public class LogFsDirFactory(IFilesystem filesystem, IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var suffix = UrlUtils.GetQueryValue(url.Query, "suffix", "");
            var autoFlush = UrlUtils.GetQueryValue(url.Query, "autoFlush", false);
            var useWriterThread = UrlUtils.GetQueryValue(url.Query, "useWriterThread", true);
            var dateTimePattern = UrlUtils.GetQueryValue(url.Query, "dateTimePattern", "yyyy-MM-dd");
            var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            var level = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            return new LogFsDir(filesystem, url.AbsolutePath, suffix, autoFlush, useWriterThread, logFormatter, dateTimePattern, level);
        }

    }

}


