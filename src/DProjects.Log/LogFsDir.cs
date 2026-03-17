using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using System;
using System.IO;
using System.Text;


namespace DProjects.Log {

    public class LogFsDir : Log {


        //variables
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly string mSuffix;
        private readonly ILogEntrySerializer mLogEntrySerializer;
        private readonly string mDateTimePattern;
        private readonly string mExtension;

        private string? mFileName;
        private TextWriter? mWriter;


        //constructor
        public LogFsDir(IFilesystem filesystem, string path, string suffix, bool autoFlush, bool useWriterThread, ILogEntrySerializer logEntrySerializer, string dateTimePattern = "yyyy-MM-dd", LogLevel level = LogLevel.Information, string extension = "log") : base(autoFlush, useWriterThread, level) {
            mFilesystem = filesystem;
            mPath = path;
            mSuffix = suffix;
            mDateTimePattern = dateTimePattern;
            mLogEntrySerializer = logEntrySerializer;
            mExtension = extension;
            if (!mFilesystem.ExistsDirectory(mPath)) {
                mFilesystem.CreateDirectory(mPath);
            }
        }
        public override void Dispose() {
            base.Dispose();
            if (mWriter != null) {
                mWriter.Dispose();
                mWriter = null;
            }
        }


        //properties
        public string Path => mPath;
        public string Suffix => mSuffix;
        public string DateTimePattern => mDateTimePattern;


        //methods
        protected override void ProcessEntry(LogEntry logEntry) {
            var newFileName = PathUtils.Combine(mPath, logEntry.Date.ToUniversalTime().ToString(mDateTimePattern) + "-Z" + mSuffix + "." + mExtension);
            if (mWriter != null && newFileName != mFileName) {
                mWriter.Dispose();
                mWriter = null;
            }
            if (mWriter == null) {
                if (!mFilesystem.ExistsFile(newFileName)) {
                    mFilesystem.SaveBinaryFile(newFileName, []);
                }
                mWriter = new StreamWriter(mFilesystem.LoadWriteStream(newFileName, new() { Truncate = false, Append = true }), new UTF8Encoding(false));
                mFileName = newFileName;
            }
            var line = mLogEntrySerializer.Serialize(logEntry);
            mWriter.WriteLine(line);
            if (mAutoFlush) mWriter.Flush();
        }

    }

}

