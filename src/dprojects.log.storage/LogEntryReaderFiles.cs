using DProjects.Fs;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderFiles : ILogEntryReader {


        //variables
        private IFilesystem mFilesystem;
        private List<string> mPaths;
        private LogFormat mLogFormat;
        private Encoding mEncoding;
        private ILogEntryReader? mLogEntryReader;

        //constructor
        public LogEntryReaderFiles(IFilesystem filesystem, string[] paths, Encoding encoding, LogFormat logFormat) {
            mFilesystem = filesystem;
            mPaths = new List<string>(paths);
            mEncoding = encoding;
            mLogFormat = logFormat;
        }
        public void Dispose() {
            if (mLogEntryReader != null) {
                mLogEntryReader.Dispose();
            }
        }


        //methods
        public LogEntry? Read() {
            if (mLogEntryReader == null) {
                if (mPaths.Count == 0) return null;
                var textReader = new StreamReader(mFilesystem.LoadReadStream(mPaths[0]));
                mLogEntryReader = new DProjects.Log.Readers.LogEntryReaderAuto(mFilesystem.LoadReadStream(mPaths[0]), mEncoding, mLogFormat);
                mPaths.RemoveAt(0);
            }
            do {
                var logEntry = mLogEntryReader.Read();
                if (logEntry == null && mPaths.Count > 0) {
                    mLogEntryReader.Dispose();
                    mLogEntryReader = null;
                    logEntry = Read();
                }
                if (logEntry == null) return null;
                return logEntry;
            } while (true);
        }
        public async Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            if (mLogEntryReader == null) {
                if (mPaths.Count == 0) return null;
                var textReader = new StreamReader(await mFilesystem.LoadReadStreamAsync(mPaths[0]));
                mLogEntryReader = new DProjects.Log.Readers.LogEntryReaderAuto(mFilesystem.LoadReadStream(mPaths[0]), mEncoding, mLogFormat);
                mPaths.RemoveAt(0);
            }
            do {
                var logEntry = await mLogEntryReader.ReadAsync(cancellationToken);
                if (logEntry == null && mPaths.Count > 0) {
                    mLogEntryReader.Dispose();
                    mLogEntryReader = null;
                    logEntry = Read();
                }
                if (logEntry == null) return null;
                return logEntry;
            } while (true);
        }
    }

}

