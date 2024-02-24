using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml;


namespace DProjects.Log {


    public class LogFsFile : Log {


        //variables
        private IFilesystem mFilesystem;
        private string mPath;
        private ILogEntrySerializer mLogEntrySerializer;
        private TextWriter mWriter;


        //constructor
        public LogFsFile(IFilesystem filesystem, string path, bool truncate, bool autoFlush, bool useWriterThread, ILogEntrySerializer logEntrySerializer, LogLevel level = LogLevel.Information) : base(autoFlush, useWriterThread, level) {
            mFilesystem = filesystem;
            mPath = path;
            mLogEntrySerializer = logEntrySerializer;
            mWriter = new StreamWriter(mFilesystem.LoadWriteStream(mPath, new() { Truncate = truncate, Append = true }), new UTF8Encoding(false));
        }
        public override void Dispose() {
            base.Dispose();
            mWriter.Dispose();
        }


        //properties
        public string Path {
            get {
                return mPath;
            }
            set {
                mPath = value;
            }
        }


        //private methods
        protected override void ProcessEntry(LogEntry logEntry) {
            var line = mLogEntrySerializer.Serialize(logEntry);
            mWriter.WriteLine(line);
            if (mAutoFlush) mWriter.Flush();
        }

    }

}

