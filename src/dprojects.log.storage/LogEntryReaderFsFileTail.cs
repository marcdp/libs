using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Log.Extensions;
using DProjects.Streams;
using DProjects.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //inner class
    public class LogEntryReaderFsFileTail : ILogEntryReader {

        //variables
        private IFilesystem mFilesystem;
        private string mPath;
        private Encoding mEncoding;
        private LogFormat mLogFormat;
        private ILogEntryReader mEntryReader;
        private int mLines;
        private bool mFollow;
        private Queue<LogEntry> mLastLogEntries;

        //constructor
        public LogEntryReaderFsFileTail(IFilesystem filesystem, string path, LogFormat logFormat, Encoding encoding, int lines, bool follow) {
            mFilesystem = filesystem;
            mPath = path;
            mLogFormat = logFormat;
            mEncoding = encoding;
            mLines = lines;
            mFollow = follow;   
            //detect format
            if (mLogFormat == LogFormat.Auto) {
                var firstLine = filesystem.LoadFirstTextLine(mPath, mEncoding);
                mLogFormat = LogFormat.Auto.DetectFormat(firstLine);
            }
            //open stream and seek
            mLastLogEntries = new Queue<LogEntry>();
            var stream = mFilesystem.LoadReadStream(mPath);
            if (lines < 0) {
                //from the beginning
                mEntryReader = new DProjects.Log.Readers.LogEntryReaderAuto(new LineDelimitedReader(new StreamReader(stream, encoding)), mLogFormat);
            } else {
                //last N lines
                var blockSize = 64 * 1024;
                if (mLogFormat.Seekable() && stream.CanSeek && stream.Length > blockSize) {
                    stream.Seek(-blockSize, SeekOrigin.End);
                    StreamUtils.ReadLine(stream, encoding);
                }
                //open reader
                var streamReader = new StreamReader(stream, encoding);
                var fullLineTextReader = new LineDelimitedReader(streamReader);
                mEntryReader = new DProjects.Log.Readers.LogEntryReaderAuto(fullLineTextReader, mLogFormat);
                //lines
                while (true) {
                    var logEntry = mEntryReader.Read();
                    if (logEntry == null) break;
                    mLastLogEntries.Enqueue(logEntry);
                    if (mLastLogEntries.Count > mLines) mLastLogEntries.Dequeue();
                }
            }
        }
        public void Dispose() {
            mEntryReader.Dispose();
        }


        //properties
        public string Path => mPath;


        //methods
        public LogEntry? Read() {
            if (mLastLogEntries.Count > 0) return mLastLogEntries.Dequeue();
            if (!mFollow) return null;
            return mEntryReader.Read();
        }
        public Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            if (mLastLogEntries.Count > 0) return Task.FromResult(mLastLogEntries.Dequeue())!;
            if (!mFollow) return Task.FromResult((LogEntry?)null);
            return mEntryReader.ReadAsync(cancellationToken);
        }

    }

}