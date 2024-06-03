using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log {

    public class LogLogger : Log,  ILog {


        //vars
        private ILogger mLogger;


        //constructor
        public LogLogger(ILogger logger, LogLevel logLevel) : base(false, false, logLevel) {
            mLogger = logger;
        }

        //methods
        protected override void ProcessEntry(LogEntry logEntry) {
            //log level
            LogLevelNative logLevelNative = LogLevelNative.Information;
            if (logEntry.Level == LogLevel.Debug) {
                logLevelNative = LogLevelNative.Debug;
            } else if (logEntry.Level == LogLevel.Trace) {
                logLevelNative = LogLevelNative.Trace;
            } else if (logEntry.Level == LogLevel.Information) {
                logLevelNative = LogLevelNative.Information;
            } else if (logEntry.Level == LogLevel.Warning) {
                logLevelNative = LogLevelNative.Warning;
            } else if (logEntry.Level == LogLevel.Error) {
                logLevelNative = LogLevelNative.Error;
            } else if (logEntry.Level == LogLevel.Fatal) {
                logLevelNative = LogLevelNative.Critical;
            }
            //message
            var message = new StringBuilder();
            message.Append(logEntry.Message);
            var args = new List<object?>();
            if (logEntry.Tags != null) {
                message.Append(" {tags}");
                args.Add(logEntry.Tags);
            }
            if (logEntry.Source != null) {
                message.Append(" {source}");
                args.Add(logEntry.Source);
            }
            if (logEntry.Fields != null) {
                foreach(var field in logEntry.Fields) {
                    message.Append(" {" + field.Key + "}");
                    args.Add(field.Value);
                }                
            }
            if (logEntry.User != null) {
                message.Append(" {user}");
                args.Add(logEntry.User);
            }
            //log
            mLogger.Log(logLevelNative, message.ToString(), args.ToArray());
        }

    }

}

