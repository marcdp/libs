using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;


namespace DProjects.Log.Serializers {


    //class
    public class LogEntrySerializerRaw : ILogEntrySerializer {


        //private 
        public string Serialize(LogEntry entry) {
            return entry.Message;
        }

    }
}

