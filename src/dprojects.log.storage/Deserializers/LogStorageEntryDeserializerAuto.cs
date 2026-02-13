using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerAuto : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            if (line.Length > 26 && DateTime.TryParseExact(line.Substring(0, 24), DateTimeUtils.DATETIME_ISO8601_MS, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime date2) && line.ToCharArray()[25] == '[') {
                //RatLog format (ex: 2020-11-14T18:12:35.352Z [info...)
                return new LogStorageEntryDeserializerRat().Deserialize(line);
            } else if (line.StartsWith("{")) {
                //json
                return new LogStorageEntryDeserializerJson().Deserialize(line);
            } else if (line.StartsWith("Debug|") || line.StartsWith("Information|") || line.StartsWith("Warning|") || line.StartsWith("Error|") || line.StartsWith("Critical|") || line.StartsWith("Severe|")) {
                //Classic format (ex: Information|2018-04-26 00:00:00 34|/campus/campusrpc.ashx?60233135-a6c8-4e5c-a042-a96a2c596e5e|campusrpc:{"id":32,"method":"/quiHiHa/admin.aspx/GetContactesOnline","params":[true],"jsonrpc":"2.0"}|0||350416|)
                return new LogStorageEntryDeserializerClassic().Deserialize(line);
            } else if (line.StartsWith("#")) {
                //W3C
                throw new NotImplementedException();
            } else if (line.StartsWith("\"")) {
                //csv
                throw new NotImplementedException();
            } else {
                //raw
                return new LogStorageEntryDeserializerRaw().Deserialize(line);
            }
        }
        public LogEntry? TryDeserialize(string line) {
            try {
                return Deserialize(line);
            } catch {
                return null;
            }
        }
    }

}

