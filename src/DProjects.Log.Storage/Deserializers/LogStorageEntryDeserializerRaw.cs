using System;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerRaw : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            // raw records have no intrinsic timestamp; the stable minimum-value sentinel participates normally in temporal queries
            var result = new LogEntry(LogLevel.Information, line);
            result.Date = DateTime.MinValue;
            return result;
        }
    }

}

