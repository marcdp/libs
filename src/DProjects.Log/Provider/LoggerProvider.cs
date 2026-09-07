using Microsoft.Extensions.Logging;
using DProjects.Factories;
using System;
using System.Threading;

namespace DProjects.Log.Provider {


    public class LoggerProvider : ILoggerProvider {

        //vars
        private ILog? mLog;

        //constructor
        public LoggerProvider(LoggerProviderConfiguration configuration, IFactoryByUrl<ILog> logFactory) {
            var log = logFactory.Create(configuration.Url);
            mLog = log;
        }
        public void Dispose() {
            Interlocked.Exchange(ref mLog, null)?.Dispose();
        }

        //methods
        public ILogger CreateLogger(string categoryName) {
            var log = mLog;
            if (log == null) {
                throw new ObjectDisposedException(nameof(LoggerProvider));
            }
            return new Logger(log, categoryName);
        }
    }


}

