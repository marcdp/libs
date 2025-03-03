using DProjects.Utils;
using System;
using System.Reflection;
using System.Text.Json.Serialization;


namespace DProjects.Commands.Schema {


    public class CmdSchemaArgument {

        //properties
        public string Name { get; set; } = "";
        [JsonIgnore] public Type Type { get; set; } = typeof(string);
        [JsonPropertyName("Type")]
        public string TypeName {
            get {
                return Type.Name;
            }
            set {
                var type = ConvertUtils.ToSimpleType(value);
                if (type == null) throw new Exception("Unable to set argument type: invalid type: " + value);
                Type = type;
            }
        }
        public int Index { get; set; } = 0;
        public string Description { get; set; } = "";
        public object? Default { get; set; }
        public bool Required { get; set; }
        public string[]? Domain { get; set; }
        public string? Alias { get; set; }
        [JsonIgnore] public PropertyInfo? PropertyInfo { get; set; }

        //constructor
        public CmdSchemaArgument() {
        }

    }

}
