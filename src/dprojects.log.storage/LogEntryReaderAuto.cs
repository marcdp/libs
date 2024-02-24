
using DProjects.Log.Extensions;
using DProjects.Streams;
using DProjects.Utils;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //interface
    public class LogEntryReaderAuto : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;
        private LogFormat mLogFormat;
        private ILogEntryReader mLogEntryReader;


        //constructor
        public LogEntryReaderAuto(Stream stream, Encoding encoding, LogFormat logFormat, bool leaveOpen = false) : this(new StreamReader(stream, encoding, true), logFormat, leaveOpen) {
        }
        public LogEntryReaderAuto(TextReader textReader, LogFormat logFormat, bool leaveOpen = false) {
            mTextReader = textReader;
            mLogFormat = logFormat;
            mLeaveOpen = leaveOpen;
            if (mLogFormat == LogFormat.Auto) {
                var lineReader = new LineReader(mTextReader, false);
                var line = lineReader.ReadLine();
                if (line!= null) {
                    lineReader.PushBackLine(line);
                    mLogFormat = LogFormat.Auto.DetectFormat(line);
                } else {
                    mLogFormat = LogFormat.Raw;
                }
                mTextReader = lineReader;
            }
            if (mLogFormat == LogFormat.Rat) {
                mLogEntryReader = new LogEntryReaderRat(mTextReader);
            } else if (mLogFormat == LogFormat.Json) {
                mLogEntryReader = new LogEntryReaderJson(mTextReader);
            } else if (mLogFormat == LogFormat.Classic) {
                mLogEntryReader = new LogEntryReaderClassic(mTextReader);
            } else if (mLogFormat == LogFormat.W3C) {
                mLogEntryReader = new LogEntryReaderW3C(mTextReader);
            } else if (mLogFormat == LogFormat.Csv) {
                mLogEntryReader = new LogEntryReaderCsv(mTextReader);
            } else {
                mLogEntryReader = new LogEntryReaderRaw(mTextReader);
            }
        }
        public void Dispose() {
            if (!mLeaveOpen) {
                mTextReader.Dispose();
            }
        }

        //properties
        public LogFormat Format => mLogFormat;


        //methods
        public LogEntry? Read() {
            return mLogEntryReader.Read();
        }
        public Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            return mLogEntryReader.ReadAsync(cancellationToken);
        }

    }


}

