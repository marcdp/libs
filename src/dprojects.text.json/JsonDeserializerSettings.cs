using System.Text.Json;

namespace DProjects.Text.Json {
    //parse
    public class JsonDeserializerSettings {
        public bool AllowTrailingCommas { get; set; }
        public bool IncludeFields { get; set; }
        public bool UseBooleanLaxConverter { get; set; }
        public bool UseDateTimeLaxConverter { get; set; }
        public bool UseIntLaxConverter { get; set; }
        public bool PropertyNameCaseInsensitive { get; set; } = true;
        public JsonNamingPolicy NamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;    
        public JsonDeserializerSettings() {
        }
    }


}
