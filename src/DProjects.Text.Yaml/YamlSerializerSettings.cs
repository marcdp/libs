using DProjects.Utils;
using System.Collections.Specialized;
using System.Reflection;

namespace DProjects.Text.Yaml {


    public class YamlSerializerSettings {

        //enum
        public enum NamingModes {
            None,
            CapitalCamelCase
        }
        public enum DefaultsModes {
            OmitDefaults,
            OmitNull,
            Preserve
        }
        public enum BinaryModes {
            Default,
            Binary,
            Base64Folded
        }

        //props
        public NamingModes NamingMode { get; set; } = NamingModes.CapitalCamelCase;
        public DefaultsModes DefaultsMode { get; set; } = DefaultsModes.Preserve;
        public BinaryModes BinaryMode { get; set; } = BinaryModes.Binary;
        public string[] Unprefixes { get; set; } = [];
        public string[] IgnorePropertyNames { get; set; } = [];
        public string[] ContentPropertyNames { get; set; } = [];
        public bool IgnoreFields { get; set; } = true;
        public bool FrontMatter { get; set; } = false;
        public bool EnableAliases { get; set; } = false;

    }

}


