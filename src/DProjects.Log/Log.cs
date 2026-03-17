
using DProjects.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;


namespace DProjects.Log {


    public abstract class Log : ILog {


        //variables
        protected readonly bool mAutoFlush;
        protected readonly bool mUseWriterThread;
        protected readonly Thread? mThread;
        protected readonly BlockingCollection<LogEntry?>? mThreadEntryQueue;
        protected readonly LogLevel mLevel;
        protected readonly int mMaxQueueLength;


        //constructor
        public Log(bool autoFlush, bool useWriterThread, LogLevel level = LogLevel.Information) {  
            mAutoFlush = autoFlush;
            mUseWriterThread = useWriterThread;
            mLevel = level;
            mMaxQueueLength = 10000;
            if (mUseWriterThread) {
                mThreadEntryQueue = new BlockingCollection<LogEntry?>();
                mThread = new Thread(new System.Threading.ThreadStart(() => {
                    do {
                        var logEntry = mThreadEntryQueue.Take();
                        if (logEntry == null) {
                            break;
                        }
                        ProcessEntry(logEntry);
                    } while (true);
                }));
                mThread.IsBackground = true;
                mThread.Start();
            }
        } 
        public virtual void Dispose() {
            if (mUseWriterThread) {
                mThreadEntryQueue?.Add(null); 
                if (mThread != null) {
                    mThread.Join();
                }
            }
        }


        //properties
        public bool AutoFlush => mAutoFlush;
        public bool UseWriterThread => mUseWriterThread;
        public LogLevel Level => mLevel;

        //methods
        public void Trace(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Write(new LogEntry(LogLevel.Trace, message, fields, tags, source, user, resource, default, spanId, traceId));
        }
        public void Debug(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Write(new LogEntry(LogLevel.Debug, message, fields, tags, source, user, resource, default, spanId, traceId));
        }
        public void Info(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Write(new LogEntry(LogLevel.Information, message, fields, tags, source, user, resource, default, spanId, traceId));
        }
        public void Warning(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Write(new LogEntry(LogLevel.Warning, message, fields, tags, source, user, resource, default, spanId, traceId));
        }
        public void Error(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
            if (exception != null) {
                if (fields == null) fields = new Dictionary<string, object?>();
                fields["exception"] = ExceptionUtils.GetMessageDetailed(exception);
            }
            Write(new LogEntry(LogLevel.Error, message, fields, tags, source, user, resource, default, spanId, traceId));
        }
        public void Fatal(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
            if (exception != null) {
                if (fields == null) fields = new Dictionary<string, object?>();
                fields["exception"] = ExceptionUtils.GetMessageDetailed(exception);
            }
            Write(new LogEntry(LogLevel.Fatal, message, fields, tags, source, user, resource));
        }
        public void Write(LogEntry logEntry) {
            if (mLevel > logEntry.Level) return;
            if (mUseWriterThread) {
                if (mThreadEntryQueue?.Count < mMaxQueueLength) {
                    mThreadEntryQueue?.Add(logEntry);
                }
            } else {
                ProcessEntry(logEntry);
            }
        }

        //to override
        protected abstract void ProcessEntry(LogEntry logEntry);


    }


}

