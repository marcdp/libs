using DProjects.Fs;
using DProjects.Utils;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DProjects.Fs.Watcher;

namespace DProjects.Log.Readers {


    public class LogEntryReaderDirTail : ILogEntryReader {


        //variables
        private IFilesystem mFilesystem;
        private string mPath;
        private Encoding mEncoding;
        private string mPattern;
        private bool mRecursive;
        private LogFormat mLogFormat;
        private int mLines;
        private bool mFollow;
        private LogEntryReaderFsFileTail? mLogEntryReader;
        private Watcher mWatcher;
        private bool mReloadLogEntryReader;

        //constructor
        public LogEntryReaderDirTail(IFilesystem filesystem, string path, string pattern, bool recursive, LogFormat logFormat, Encoding encoding, int lines, bool follow) {
            mFilesystem = filesystem;
            mPath = path;
            mPattern = pattern;
            mRecursive = recursive;
            mLogFormat = logFormat;
            mEncoding = encoding;
            mLines = lines;
            mFollow = follow;
            mReloadLogEntryReader = false;
            mWatcher = filesystem.CreateWatcher(mPath, pattern, ConstantsUtils.EMPTY_STRING_ARRAY, recursive);
            mWatcher.Changed += (sender, changeType, path) => {
                if (changeType == ChangeType.Created) {
                    mReloadLogEntryReader = true;
                }
            };
        }
        public void Dispose() {
            mWatcher.Dispose();
            mLogEntryReader?.Dispose();
        }


        //methods
        public LogEntry? Read() {
            if (mLogEntryReader == null) InitLogEntryReader();
            if (mLogEntryReader == null) return null;
            var logEntry = mLogEntryReader.Read();
            if (logEntry == null && mReloadLogEntryReader) {
                InitLogEntryReader();
            }
            return logEntry;
        }
        public async Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            if (mLogEntryReader == null) InitLogEntryReader();
            if (mLogEntryReader == null) return (LogEntry?)null;
            var logEntry = await mLogEntryReader.ReadAsync(cancellationToken);
            if (logEntry == null && mReloadLogEntryReader) {
                InitLogEntryReader();
            }
            return logEntry;
        }

         
        //private
        private void InitLogEntryReader() {
            mReloadLogEntryReader = false;
            lock (this) {
                string? lastPath = null;
                foreach (var entry in mFilesystem.GetEntries(mPath, (mRecursive ? GetModes.Descendants : GetModes.Files), mPattern)) {
                    if (entry.IsFile()) {
                        lastPath = entry.Path;
                    }
                }
                if (lastPath != null) {
                    if (mLogEntryReader != null) {
                        if (!mLogEntryReader.Path.Equals(lastPath)) {
                            mLogEntryReader.Dispose();
                            mLogEntryReader = new LogEntryReaderFsFileTail(mFilesystem, lastPath, mLogFormat, mEncoding, -1, mFollow);
                        }
                    } else {
                        mLogEntryReader = new LogEntryReaderFsFileTail(mFilesystem, lastPath, mLogFormat, mEncoding, mLines, mFollow);
                    }
                }
            }
        }
    }

}