using System;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerRaw : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            // raw records do not contain timestamps, so use a stable unknown sentinel rather than the read time
            var result = new LogEntry(LogLevel.Information, line);
            result.Date = DateTime.MinValue;
            return result;
        }
    }

}

