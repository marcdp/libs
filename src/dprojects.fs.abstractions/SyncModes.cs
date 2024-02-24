

using System.Text.Json.Serialization;

namespace DProjects.Fs {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SyncModes {
        LeftToRight,
        Bidirectional
    }


}