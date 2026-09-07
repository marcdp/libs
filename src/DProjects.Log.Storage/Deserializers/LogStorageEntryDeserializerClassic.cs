using DProjects.Utils;
using System;
using System.Collections.Generic;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerClassic : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            //Ex: Information|2018-04-26 00:00:00 34|/campus/campusrpc.ashx?60233135-a6c8-4e5c-a042-a96a2c596e5e|campusrpc:{"id":32,"method":"/quiHiHa/admin.aspx/GetContactesOnline","params":[true],"jsonrpc":"2.0"}|0||350416|
            if (line == null) throw new ArgumentNullException(nameof(line));
            try {
                string[] parts = line.Split('|');
                if (!TryGetLevel(parts[0], out var logType)) throw new FormatException("The Classic log level is not supported.");
                var aDate = DateTimeUtils.Parse(parts[1], true).ToUniversalTime();
                string message = parts[3].Replace("\\r", CharUtils.CHAR_CR.ToString()).Replace("\\n", CharUtils.CHAR_LF.ToString()).Replace("\\u007C", "|").Replace("\\\\", "\\");
                var fields = new Dictionary<string, object?>();
                if (parts.Length > 4 && !parts[4].Equals("0")) fields["extra"] = parts[4];
                string source = parts[2];
                string hostname = (parts.Length > 5 ? (parts[5]) : "");
                if (!String.IsNullOrEmpty(hostname)) source += "," + hostname;
                string user = (parts.Length > 6 ? (parts[6]) : "");
                return new LogEntry(logType, message, fields, null, source, user, null, aDate);
            } catch (Exception e) {
                throw new FormatException("Unable to parse Classic log record.", e);
            }
        }

        // methods (private)
        internal static bool LooksLikeRecord(string line) {
            var firstSeparator = line.IndexOf('|');
            if (firstSeparator <= 0) return false;
            var secondSeparator = line.IndexOf('|', firstSeparator + 1);
            if (secondSeparator <= firstSeparator + 1) return false;
            var thirdSeparator = line.IndexOf('|', secondSeparator + 1);
            if (thirdSeparator == -1) return false;
            return DateTimeUtils.TryParse(line.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1), out _);
        }
        private static bool TryGetLevel(string value, out LogLevel level) {
            switch (value) {
                case "Trace": level = LogLevel.Trace; return true;
                case "Debug": level = LogLevel.Debug; return true;
                case "Information": level = LogLevel.Information; return true;
                case "Warning": level = LogLevel.Warning; return true;
                case "Error": level = LogLevel.Error; return true;
                case "Critical":
                case "Fatal":
                case "Severe": level = LogLevel.Fatal; return true;
                default: level = default; return false;
            }
        }
    }

}

