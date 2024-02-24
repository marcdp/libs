using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerRaw : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            return new LogEntry(LogLevel.Information, line, null, null, null, null, DateTime.Now);
        }
    }

}

