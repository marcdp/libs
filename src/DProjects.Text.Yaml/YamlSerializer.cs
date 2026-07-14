using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DProjects.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;
using static DProjects.Text.Yaml.YamlSerializerSettings;
namespace DProjects.Text.Yaml {


    public class YamlSerializer(YamlSerializerSettings settings) : DProjects.Serialization.ISerializer  {


        //methods
        public void Serialize(object value, Stream stream, Encoding encoding) {
            using var writer = new StreamWriter(stream, encoding, 1024, true);
            Serialize(value, writer);
        }
        public string Serialize(object? value) {
            var sw = new StringWriter();
            Serialize(value, sw);
            return sw.ToString();
        }
        public void Serialize(object? value, TextWriter writer) {
            if (value == null) {
                //ContentNodes
                if (settings.FrontMatter) writer.WriteLine("---");
                writer.Write("~");
                if (settings.FrontMatter) writer.WriteLine("---");
            } else {
                var serializerBuilder = new SerializerBuilder();
                //naming mode
                if (settings.NamingMode == YamlSerializerSettings.NamingModes.None) {
                } else if (settings.NamingMode == YamlSerializerSettings.NamingModes.CapitalCamelCase) {
                    serializerBuilder = serializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
                }
                //default mode
                if (settings.DefaultsMode == YamlSerializerSettings.DefaultsModes.OmitDefaults) {
                    serializerBuilder = serializerBuilder.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults);
                } else if (settings.DefaultsMode == YamlSerializerSettings.DefaultsModes.OmitNull) {
                    serializerBuilder = serializerBuilder.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull);
                } else if (settings.DefaultsMode == YamlSerializerSettings.DefaultsModes.Preserve) {
                    serializerBuilder = serializerBuilder.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve);
                }
                //binary converter
                if (settings.BinaryMode == YamlSerializerSettings.BinaryModes.Default) {
                } else if (settings.BinaryMode == YamlSerializerSettings.BinaryModes.Binary) {
                    serializerBuilder = serializerBuilder.WithTypeConverter(new YamlConverters.ByteArrayConverter()).WithTagMapping("tag:yaml.org,2002:binary", typeof(byte[]));
                } else if (settings.BinaryMode == YamlSerializerSettings.BinaryModes.Base64Folded) {
                    serializerBuilder = serializerBuilder.WithTypeConverter(new YamlConverters.ByteArrayBase64FoldedConverter());
                }
                //ignore fields
                if (settings.IgnoreFields) {
                    serializerBuilder = serializerBuilder.IgnoreFields();
                }
                //ignore property names
                if (settings.IgnorePropertyNames.Length > 0 || settings.ContentPropertyNames.Length > 0) {
                    var ignoredPropertyNames = new List<string>();
                    ignoredPropertyNames.AddRange(settings.IgnorePropertyNames);
                    if (settings.FrontMatter) ignoredPropertyNames.AddRange(settings.ContentPropertyNames); 
                    serializerBuilder = serializerBuilder.WithTypeInspector(inspector => 
                        new MyTypeInspectorIgnoreProperties(inspector, ignoredPropertyNames.ToArray(), value)
                    );
                    serializerBuilder = serializerBuilder.WithTypeConverter(new YamlConverters.DictionaryStringObjectConverter(ignoredPropertyNames.ToArray()));
                }
                // serializer
                if (!settings.EnableAliases) {
                    serializerBuilder.DisableAliases();
                }
                //build
                var serializer = serializerBuilder.Build();
                //serialize
                if (settings.FrontMatter) writer.WriteLine("---");
                serializer.Serialize(writer, value);
                if (settings.FrontMatter) writer.WriteLine("---");
                //content nodes
                if (settings.FrontMatter && settings.ContentPropertyNames.Length > 0) {
                    int index = 0;
                    var contentValues = new Dictionary<string, object?>();
                    //get content values
                    if (value is IDictionary<string, object?> dict) {
                        foreach (var propertyName in settings.ContentPropertyNames) {
                            if (dict.TryGetValue(propertyName, out var propertyValue)) {
                                contentValues.Add(propertyName, propertyValue);
                            }
                        }
                    } else {
                        foreach (var propertyName in settings.ContentPropertyNames) {
                            var propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                            if (propertyInfo != null) {
                                var propertyValue = propertyInfo.GetValue(value, null);
                                contentValues.Add(propertyName, propertyValue);
                            }
                        }
                    }
                    //render                        
                    foreach (var contentValue in contentValues) {
                        if (index++ > 0) {
                            writer.WriteLine("---" + contentValue.Key);
                        }
                        if (contentValue.Value is string) {
                            writer.WriteLine(contentValue.Value);
                        } else if (contentValue.Value is byte[]) {
                            if (settings.BinaryMode == BinaryModes.Default) {
                                serializer.Serialize(writer, contentValue.Value);
                            } else if (settings.BinaryMode == BinaryModes.Default) {
                                writer.Write("!!binary " + Convert.ToBase64String((byte[])contentValue.Value));
                            } else {
                                writer.Write(StringUtils.SplitByColumnsAndFold(Convert.ToBase64String((byte[])contentValue.Value), 76));
                            }
                        } else {
                            serializer.Serialize(writer, contentValue.Value);
                        }
                    }
                }
            }
        }


        //utils
        private class MyTypeInspectorIgnoreProperties : TypeInspectorSkeleton {
            private readonly ITypeInspector mInnerTypeDescriptor;
            private readonly string[] mIgnorePropertyNames;
            private readonly object mRoot;
            public MyTypeInspectorIgnoreProperties(ITypeInspector innerTypeDescriptor, string[] ignorePropertyNames, object root) {
                mInnerTypeDescriptor = innerTypeDescriptor;
                mIgnorePropertyNames = ignorePropertyNames;
                mRoot = root;
            }

            public override string GetEnumName(Type enumType, string name) {
                throw new NotImplementedException();
            }

            public override string GetEnumValue(object enumValue) {
                throw new NotImplementedException();
            }

            public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) {
                var props = mInnerTypeDescriptor.GetProperties(type, container);
                if (mRoot == container) {
                    props = props.Where(p => !(p.Type == typeof(Dictionary<string, object>) && p.Name == "extensions"));
                    props = props.Where((p) => {
                        return System.Array.IndexOf(mIgnorePropertyNames, p.Name) == -1;
                    });
                }
                return props;
            }
        }

    }

}


