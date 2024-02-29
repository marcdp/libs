using DProjects.Utils;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DProjects.Text.Yaml {


    public class YamlDeserializer(YamlDeserializerSettings settings) : DProjects.Serialization.IDeserializer {


        //methods
        public T Deserialize<T>(Stream stream, Encoding encoding) {
            using var reader = new StreamReader(stream, encoding, false, 1024, true);
            return Deserialize<T>(reader);
        }
        public T Deserialize<T>(string input) {
            return Deserialize<T>(new StringReader(input));
        }
        public T Deserialize<T>(TextReader reader) {
            return (T)Deserialize(typeof(T), reader);
        }
        public object Deserialize(Type type, TextReader reader) {
            if (settings.ContentNodes) {
                var yaml = InlineContentNodes(reader);
                reader = new StringReader(yaml);
            } else {
                var yaml = reader.ReadToEnd().Trim();
                if (yaml.EndsWith("---\n")) {
                    yaml = yaml.Substring(0, yaml.Length - 4);
                } else if (yaml.EndsWith("---\r\n")) {
                    yaml = yaml.Substring(0, yaml.Length - 5);
                } else if (yaml.EndsWith("---")) { 
                    yaml = yaml.Substring(0, yaml.Length - 3);
                }
                reader = new StringReader(yaml);
            }
            if (type == typeof(YamlDotNet.RepresentationModel.YamlDocument)) {
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);
                var documents = yamlStream.Documents;
                var document = documents[0];
                return document;
            } else {
                var deserializerBuilder = new DeserializerBuilder();
                if (settings.NamingMode == YamlDeserializerSettings.NamingModes.None) {
                } else if (settings.NamingMode == YamlDeserializerSettings.NamingModes.CapitalCamelCase) {
                    deserializerBuilder = deserializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
                }
                if (settings.AutoDetectScalars) deserializerBuilder.WithNodeTypeResolver(new ScalarNodeTypeResolver());
                deserializerBuilder = deserializerBuilder.WithTypeConverter(new YamlConverters.ByteArrayConverter()).WithTagMapping("tag:yaml.org,2002:binary", typeof(byte[]));
                var deserializer = deserializerBuilder.Build();
                return deserializer.Deserialize(reader, type)!;
            }
        }
        public string InlineContentNodes(TextReader reader) {
            var aux = new StringBuilder();
            var line = reader.ReadLine();
            var variable = "";
            var value = new StringBuilder();
            while (line != null) {
                if (line.StartsWith("---") && aux.Length > 0) {
                    if (variable.Length > 0) aux.AppendLine(variable + ": " + JsonSerializer.Serialize(value.ToString()));
                    value.Clear();
                    variable = line.Substring(3).Trim();
                    if (variable.Length == 0) variable = "content";
                } else if (variable.Length > 0) {
                    if (value.Length > 0) value.AppendLine();
                    value.Append(line.Replace(" ---", "---"));
                } else {
                    aux.AppendLine(line);
                }
                line = reader.ReadLine();
            };
            if (variable.Length > 0 && value.Length > 0) aux.AppendLine(variable + ": " + JsonSerializer.Serialize(value.ToString()));
            aux.Append("...");
            return aux.ToString();
        }

        //inner classes
        public class ScalarNodeTypeResolver : INodeTypeResolver {
            public bool Resolve(NodeEvent? nodeEvent, ref Type currentType) {
                if (currentType == typeof(object)) {
                    var scalar = nodeEvent as Scalar;
                    if (scalar != null) {
                        if (scalar.Value.Equals("true", StringComparison.OrdinalIgnoreCase) || scalar.Value.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                            currentType = typeof(bool);
                            return true;
                        } else if (StringUtils.IsInteger(scalar.Value) || StringUtils.IsHexadecimalInt(scalar.Value)) {
                            currentType = typeof(int);
                            return true;
                        } else if (StringUtils.IsLong(scalar.Value) || StringUtils.IsHexadecimalLong(scalar.Value)) {
                            currentType = typeof(long);
                            return true;
                        } else if (StringUtils.IsDate(scalar.Value)) {
                            currentType = typeof(DateTime);
                            return true;
                        } else if (StringUtils.IsNumeric(scalar.Value)) {
                            currentType = typeof(float);
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }

}


