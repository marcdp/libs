
using System.Text.Json.Serialization;

namespace DProjects.Fs {

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Features {
        Touch,
        CreateWatcher,
        Metadata,
        Select
    }


} 