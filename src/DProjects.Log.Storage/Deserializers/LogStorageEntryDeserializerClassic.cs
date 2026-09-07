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
                var logType = LogLevel.Information;
                if (parts[0].Equals("Trace")) logType = LogLevel.Trace;
                if (parts[0].Equals("Debug")) logType = LogLevel.Debug;
                if (parts[0].Equals("Information")) logType = LogLevel.Information;
                if (parts[0].Equals("Warning")) logType = LogLevel.Warning;
                if (parts[0].Equals("Error")) logType = LogLevel.Error;
                if (parts[0].Equals("Fatal") || parts[0].Equals("Severe")) logType = LogLevel.Fatal;
                var aDate = DateTimeUtils.Parse(parts[1], true).ToUniversalTime();
                string message = parts[3].Replace("\\r", CharUtils.CHAR_CR.ToString()).Replace("\\n", CharUtils.CHAR_LF.ToString()).Replace("\\u007C", "|").Replace("\\\\", "\\");
                var fields = new Dictionary<string, object?>();
                if (parts.Length > 4 && !parts[4].Equals("0")) fields["extra"] = parts[4];
                string source = parts[2];
                string hostname = (parts.Length > 5 ? (parts[5]) : "");
                if (!String.IsNullOrEmpty(hostname)) source += "," + hostname;
                string user = (parts.Length > 6 ? (parts[6]) : "");
                return new LogEntry(logType, message, fields, null, source, user, null, aDate);
            } catch (Exception e) when (!(e is FormatException)) {
                throw new FormatException("Unable to parse classic log record " + GetSafeContext(line) + ".", e);
            }
        }

        // methods (private)
        private static string GetSafeContext(string line) {
            const int maxLength = 120;
            return line.Length <= maxLength ? "'" + line + "'" : "'" + line.Substring(0, maxLength) + "...'";
        }
    }

}

