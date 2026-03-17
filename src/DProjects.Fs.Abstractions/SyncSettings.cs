
using Microsoft.Extensions.Logging;

namespace DProjects.Fs {


    public class SyncSettings {


        //constructor
        public SyncSettings() { }

        //properties
        public SyncModes Mode { get; set; } = SyncModes.LeftToRight;
        public string[] SourceExcludes { get; set; } = [];
        public string[] DestinationExcludes { get; set; } = [];
        public string StatusPath { get; set; } = "";
        public bool IgnoreErrors { get; set; } = false;
        public bool Recursive { get; set; } = true;
        public int Tries { get; set; } = 1;
    }


}