using System;
using System.Collections.Generic;


namespace DProjects.Log {

    //interface
    public interface ILogClient {

        //properties
        string? Prefix { get; set; }
        string? User { get; set; }
        string? Source { get; set; }
        string[]? Tags { get; set; }
        Dictionary<string, object?>? Fields { get; set; }

        //methods
        void Debug(string message, params object?[] args);
        void Info(string message, params object?[] args);
        void Warning(string message, params object?[] args);
        void Error(string message, params object?[] args);
        void Critical(string message, params object?[] args);

    }

    //generic interface
    public interface ILogClient<T> : ILogClient where T : class {
    }
    

}

