using System;
using System.Collections.Generic;


namespace DProjects.Log {

    //interface
    public interface ILogClient {

        //events
        event EventHandler<LogEntry> Writed;

        //properties
        LogLevel Level { get; }
        string? Prefix { get; set; }
        string? User { get; set; }
        string? Source { get; set; }
        string[]? Tags { get; set; }
        Dictionary<string, object?>? Fields { get; set; }
        string? SpanId { get; set; }
        string? TraceId { get; set; }

        //methods
        void Debug(string message, params object?[] args);
        void Info(string message, params object?[] args);
        void Warning(string message, params object?[] args);
        void Error(string message, params object?[] args);
        void Fatal(string message, params object?[] args);
        void Write(LogEntry logEntry);

    }

    //generic interface
    public interface ILogClient<T> : ILogClient where T : class {
    }
    

}

