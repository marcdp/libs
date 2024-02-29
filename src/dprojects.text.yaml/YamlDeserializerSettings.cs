using DProjects.Utils;

namespace DProjects.Text.Yaml {
    
    public class YamlDeserializerSettings {

        //enums
        public enum NamingModes {
            None,
            CapitalCamelCase
        }

        //props
        public bool ExpectFrontMatter { get; set; }
        public NamingModes NamingMode { get; set; } = NamingModes.CapitalCamelCase;
        public bool AutoDetectScalars { get; set; } = false;
        public bool ContentNodes { get; set; } = false;
        

    }
    

}


