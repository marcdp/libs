using DProjects.Utils;
using System;
using System.Reflection;
using System.Text.Json.Serialization;


namespace DProjects.CommandLine.Schema {


    public class CmdSchemaRaw {

        //properties
        public string Description { get; set; } = "";
        public string? Default { get; set; } = "";
        [JsonIgnore] public Type Type { get; set; } = typeof(string);
        [JsonPropertyName("Type")]
        public string TypeName {
            get {
                return Type.Name;
            }
            set {
                var type = ConvertUtils.ToSimpleType(value);
                if (type == null) throw new Exception("Unable to set raw type: invalid type: " + value);
                Type = type;
            }
        }
        public bool AvoidExpansions { get; set; }
        [JsonIgnore] public PropertyInfo? PropertyInfo { get; set; }

        //constructor
        public CmdSchemaRaw() {
        }

    }

}
