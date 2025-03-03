using System.Reflection;
using System.Text.Json.Serialization;

namespace DProjects.Commands.Schema {


    public class CmdSchemaBody {

        //properties
        public string Description { get; set; } = "";
        [JsonIgnore] public PropertyInfo? PropertyInfo { get; set; }

        //constructor
        public CmdSchemaBody() {
        }

    }

}
