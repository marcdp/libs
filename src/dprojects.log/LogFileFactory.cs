using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log {

    [Protocol("file", "")]
    [ProtocolUsage("file://PATH-TO-FILE?format=FORMAT")]
    public class LogFileFactory(IFactoryByUrl<IFilesystem> filesystemFactory, IFactoryByUrl<ILogEntrySerializer> logFormatterFactory) : IFactoryByUrl<ILog> {

        public ILog Create(string src) {
            var url = new Uri(src);
            var type = UrlUtils.GetQueryValue(url.Query, "type", "dir");
            var truncate = UrlUtils.GetQueryValue(url.Query, "truncate", false);
            var init = UrlUtils.GetQueryValue(url.Query, "init", false);
            var suffix = UrlUtils.GetQueryValue(url.Query, "suffix", "");
            var autoFlush = UrlUtils.GetQueryValue(url.Query, "autoFlush", false);
            var useWriterThread = UrlUtils.GetQueryValue(url.Query, "useWriterThread", true);
            var dateTimePattern = UrlUtils.GetQueryValue(url.Query, "dateTimePattern", "yyyy-MM-dd");
            var extension = UrlUtils.GetQueryValue(url.Query, "extension", "log");
            var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            var level = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            var path = url.AbsolutePath;
            if (DProjects.Utils.EnvironmentUtils.IsWindows()) {
                path = path[1] + ":" + path.Substring(2).Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            var filesystem = filesystemFactory.Create(path + (init ? "?init=true" : "") );
            if (type.Equals("dir")) {
                return new LogFsDir(filesystem, "/", suffix, autoFlush, useWriterThread, logFormatter, dateTimePattern, level, extension);
            } else if (type.Equals("file")) {
                return new LogFsFile(filesystem, url.AbsolutePath, truncate, autoFlush, useWriterThread, logFormatter, level);
            } else {
                throw new ArgumentException("Invalid type: " + type);
            }
        }

    }

}


