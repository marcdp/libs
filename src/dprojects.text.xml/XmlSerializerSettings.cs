using DProjects.Utils;
using System.Collections.Specialized;
using System.Reflection;

namespace DProjects.Text.Xml {


    public class XmlSerializerSettings {


        //enum
        public enum NamingModes {
            None,
            UncapitalCamelCase
        }

        //props
        public bool OmitXmlDeclaration { get; set; } = false;
        public NamingModes NamingMode { get; set; } = NamingModes.UncapitalCamelCase;
        public string[] Unprefixes { get; set; } = [];
        public NameValueCollection Alias { get; set; } = new NameValueCollection();
        public bool AvoidEmptyStrings { get; set; } = true;
        public bool AvoidFalseBooleans { get; set; } = true;
        public bool AvoidZeroNumbers { get; set; } = true;
        public bool AvoidDefaultEnumValues { get; set; } = true;
        public bool AvoidEmptyArrays { get; set; } = true;
        public string ContentPropertyName { get; set; } = "Content";

        //methods
        public string ProcessName(string name) {
            foreach (var unprefix in Unprefixes) {
                if (name.StartsWith(unprefix) && name.Length > unprefix.Length) {
                    name = name.Substring(unprefix.Length);
                    break;
                }
            }
            if (NamingMode == NamingModes.UncapitalCamelCase) {
                name = StringUtils.UnCapitalizeFirstChar(name);
            }
            if (Alias[name] != null) name = Alias[name];
            return name;
        }
        public bool IsSerializable(object instance, PropertyInfo propertyInfo) {
            if (propertyInfo.PropertyType == typeof(PropertyInfo) || propertyInfo.PropertyType == typeof(MethodInfo)) return false;
            return true;
        }

    }

}


