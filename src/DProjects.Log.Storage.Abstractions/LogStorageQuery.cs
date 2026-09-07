
using System;

namespace DProjects.Log.Storage {

    public class LogStorageQuery {

        // props
        /// <summary>Inclusive lower timestamp bound, or null for no lower bound.</summary>
        public DateTime? From { get; set; }
        /// <summary>Inclusive upper timestamp bound, or null for no upper bound.</summary>
        public DateTime? To { get; set; }
        /// <summary>Minimum severity to include.</summary>
        public LogLevel Level { get; set; } = LogLevel.Information;
        /// <summary>Ordinal message substring to match. Null disables this filter.</summary>
        public string? Message { get; set; }
        /// <summary>Exact tag to match. Null disables this filter.</summary>
        public string? Tag { get; set; }
        /// <summary>Exact source to match. Null disables this filter.</summary>
        public string? Source { get; set; }
        /// <summary>Exact user to match. Null disables this filter.</summary>
        public string? User { get; set; }

        // ctor
        public LogStorageQuery() {
        }

        // methods
        public bool Check(LogEntry logEntry) {
            if (logEntry == null) throw new ArgumentNullException(nameof(logEntry));
            if (From.HasValue && To.HasValue && From.Value > To.Value) throw new ArgumentException("From must be earlier than or equal to To.");
            if (From.HasValue && logEntry.Date < From.Value) return false;
            if (To.HasValue && logEntry.Date > To.Value) return false;
            if (logEntry.Level < Level) return false;
            if (Message != null && (logEntry.Message == null || logEntry.Message.IndexOf(Message, StringComparison.Ordinal) == -1)) return false;
            if (Tag != null && (logEntry.Tags == null || Array.IndexOf(logEntry.Tags, Tag) == -1)) return false;
            if (Source != null && !string.Equals(logEntry.Source, Source, StringComparison.Ordinal)) return false;
            if (User != null && !string.Equals(logEntry.User, User, StringComparison.Ordinal)) return false;
            return true;
        }
    }

}

