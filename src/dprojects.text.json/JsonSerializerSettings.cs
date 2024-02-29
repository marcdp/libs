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
        public JsonNamingPolicy? NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
    }


}
