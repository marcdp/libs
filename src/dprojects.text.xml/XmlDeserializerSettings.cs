using DProjects.Utils;

namespace DProjects.Text.Xml {
    
    public class XmlDeserializerSettings {

        //enums
        public enum NamingModes {
            None,
            CapitalCamelCase
        }

        //props
        public NamingModes NamingMode { get; set; } = NamingModes.CapitalCamelCase;
        public string TypePrefix { get; set; } = "";
        public bool RequireAllProperties { get; set; } = false;
        public bool IgnoreNamespaces { get; set; } = false;
        public string ContentPropertyName { get; set; } = "Content";
        

        //methods
        public string ProcessTypeName(string name) {
            if (NamingMode == NamingModes.CapitalCamelCase) {
                name = StringUtils.CapitalizeFirstChar(name);
            }
            return TypePrefix + name;
        }
        public string ProcessPropertyName(string name) {
            if (NamingMode == NamingModes.CapitalCamelCase) {
                name = StringUtils.CapitalizeFirstChar(name);
            }
            if (name.IndexOf("-") != -1) name = name.Replace("-", "");
            return name;
        }
    }
    

}


