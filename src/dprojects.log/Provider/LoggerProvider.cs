using Microsoft.Extensions.Logging;
using DProjects.Factories;

namespace DProjects.Log.Provider {


    public class LoggerProvider : ILoggerProvider {

        //vars
        private Logger mLogger;

        //constructor
        public LoggerProvider(LoggerProviderConfiguration configuration, IFactoryByUrl<ILog> logFactory) {
            var log = logFactory.Create(configuration.Url);
            mLogger = new Logger(log);
        }
        public void Dispose() {
            mLogger.Dispose();
        }

        //methods
        public ILogger CreateLogger(string categoryName) {
            return mLogger;
        }
    }


}

