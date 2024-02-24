using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("fs-dir", "")]
    [ProtocolUsage("fs-dir://PATH-TO-FILE?format=FORMAT")]
    [ProtocolExample("fs-file:///var/log/folder.log?autoFlush=true&useWriterThread=true&logFormatter=rat&level=debug&dateTimePattern=YYYYMMDD", "")]
    public class LogFsDirFactory(IFilesystem filesystem, IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var suffix = UrlUtils.ParseQueryString(url.Query, "suffix", "");
            var autoFlush = UrlUtils.ParseQueryString(url.Query, "autoFlush", false);
            var useWriterThread = UrlUtils.ParseQueryString(url.Query, "useWriterThread", true);
            var dateTimePattern = UrlUtils.ParseQueryString(url.Query, "dateTimePattern", "yyyy-MM-dd");
            var logFormatter = logFormatterFactory.Create(UrlUtils.ParseQueryString(url.Query, "format", "json"));
            var level = UrlUtils.ParseQueryString(url.Query, "level", LogLevel.Information);
            return new LogFsDir(filesystem, url.AbsolutePath, suffix, autoFlush, useWriterThread, logFormatter, dateTimePattern, level);
        }

    }

}


