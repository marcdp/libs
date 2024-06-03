using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace DProjects.Log {

    public class LogEntry {


        //properties
        public DateTime Date { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string? Resource { get; set; }
        public IDictionary<string, object?>? Fields { get; set; }
        public string[]? Tags { get; set; }
        public string? Source { get; set; }
        public string? User { get; set; }


        //constructor
        public LogEntry() {
            Date = DateTime.Now;
            Level = LogLevel.Information;
            Message = "";
        }
        public LogEntry(LogLevel logLevel, string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = "", string? user = "", string? resource = null, DateTime aDate = default) {
            if (aDate == default) aDate = DateTime.MinValue;
            this.Date = (aDate == default) ? DateTime.Now : aDate;
            this.Message = message;
            this.Resource = resource;
            this.Source = source;
            this.User = user;
            this.Level = logLevel;
            this.Fields = fields;
            this.Tags = tags;
        }


        
         
    }
}


