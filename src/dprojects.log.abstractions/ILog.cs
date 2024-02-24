using System;
using System.Collections.Generic;


namespace DProjects.Log {

    //interface
    public interface ILog : IDisposable { 


        //properties
        LogLevel Level { get; }

        //methods
        void Trace(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null);
        void Debug(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null);
        void Info(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null);
        void Warning(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null);
        void Error(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, Exception? exception = null);
        void Critical(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, Exception? exception = null);
        void Write(LogEntry logEntry);


    }


}

