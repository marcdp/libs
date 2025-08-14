using System.Text.Json;

namespace DProjects.Text.Json {
    //settings
    public class JsonSerializerSettings {
        public JsonSerializerSettings() {
        }
        public bool WriteIndented { get; set; }
        public bool IgnoreReadOnlyProperties { get; set; }
        public bool IgnoreNullValues { get; set; }
        public bool IgnoreDefaultValues { get; set; }
        public bool UseJsonStringEnumConverter { get; set; } = false;
        public bool UseDateTimeLaxConverter { get; set; } = false;
        public string UseDateTimeOffsetConverterFormat { get; set; } = string.Empty;
        public JsonNamingPolicy? NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
    }


}
