
using System;
using System.Collections.Generic;

namespace DProjects.Log {

    public class LogStorageQuery {

        //constructor
        public LogStorageQuery() {}

        //properties
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string? Message { get; set; }
        public string? Tag { get; set; }
        public string? Source { get; set; }
        public string? User { get; set; }

        //method
        public bool Check(LogEntry logEntry) {
            if (From.HasValue && From != default(DateTime) && logEntry.Date < From) return false;
            if (To.HasValue && To != default(DateTime) && logEntry.Date > To) return false;
            if (logEntry.Level < LogLevel) return false;
            if (Message != null && logEntry.Message.IndexOf(Message)==-1) return false;
            if (!string.IsNullOrEmpty(Tag) && (logEntry.Tags == null || System.Array.IndexOf(logEntry.Tags, Tag)==-1)) return false;
            if (!string.IsNullOrEmpty(Source) && (logEntry.Source == null || !logEntry.Source.Equals(Source))) return false;
            if (!string.IsNullOrEmpty(User) && (logEntry.User == null || !logEntry.User.Equals(User))) return false;
            return true;
        }
    }

}

