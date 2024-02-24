
namespace DProjects.Log {

    public class LogStdout(ILogEntrySerializer logEntrySerializer, LogLevel level = LogLevel.Information) : Log(false, false, level) {


        //private methods
        protected override void ProcessEntry(LogEntry logEntry) {
            System.Console.Out.WriteLine(logEntrySerializer.Serialize(logEntry));
        }

    }

}

