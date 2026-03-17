using System.IO;


namespace DProjects.Log {


    public class LogTextWriter : Log {


        //variables
        private ILogEntrySerializer mLogEntrySerializer;
        private TextWriter mWriter;


        //constructor
        public LogTextWriter(TextWriter writer, bool autoFlush, bool useWriterThread, ILogEntrySerializer logEntrySerializer, LogLevel level = LogLevel.Information) : base(autoFlush, useWriterThread, level) {
            mWriter = writer;
            mLogEntrySerializer = logEntrySerializer;
        }
        public override void Dispose() {
            base.Dispose();
            mWriter.Dispose();
        }


        //private methods
        protected override void ProcessEntry(LogEntry logEntry) {
            var line = mLogEntrySerializer.Serialize(logEntry);
            mWriter.WriteLine(line);
            if (mAutoFlush) mWriter.Flush();
        }

    }

}

