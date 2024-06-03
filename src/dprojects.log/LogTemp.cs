using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml;


namespace DProjects.Log {


    public class LogTemp : Log {


        //variables
        private string mPath;
        private ILogEntrySerializer mLogEntrySerializer;
        private TextWriter mWriter;
        private Action<LogEntry>? mCallback;

        //constructor
        public LogTemp(string? path = null, bool autoFlush = false, bool useWriterThread = false, ILogEntrySerializer? logEntrySerializer = null, LogLevel level = LogLevel.Information, Action<LogEntry>? callback = null) : base(autoFlush, useWriterThread, level) {
            mPath = path ?? System.IO.Path.GetTempFileName() + System.Guid.NewGuid().ToString() + ".log";
            mLogEntrySerializer = logEntrySerializer ?? new Serializers.LogEntrySerializerJson();
            mWriter = new StreamWriter(new FileStream(mPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
            mCallback = callback;
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

        //method
        public override string ToString() {
            // TODO ....
            return "";
        }
        //private methods
        protected override void ProcessEntry(LogEntry logEntry) {
            mCallback?.Invoke(logEntry);
            var line = mLogEntrySerializer.Serialize(logEntry);
            mWriter.WriteLine(line);
            if (mAutoFlush) mWriter.Flush();
        }

    }

}

