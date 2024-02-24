
using System.Text.Json.Serialization;

namespace DProjects.Fs {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Flags {
        None = 0,
        Readonly = 1,
        Hidden = 2
    }

}