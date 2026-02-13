using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using DProjects.DataObjects;
using DProjects.DataTypes;
using DProjects.Utils;

namespace DProjects.Text.Xml {


    public class XmlSerializer(XmlSerializerSettings settings) : DProjects.Serialization.ISerializer {


        //methods
        public void Serialize(object value, Stream stream, Encoding encoding) {
            using var writer = new StreamWriter(stream, encoding, 1024, true);
            Serialize(writer, value);
        }
        public XmlDocument SerializeToXmlDocument(object instance) {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(SerializeToStringUTF8NoBom(instance));
            return xmlDocument;
        }
        public string SerializeToStringUTF8(object instance) {
            if (settings == null) settings = new XmlSerializerSettings();
            var ms = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = System.Text.Encoding.UTF8;
            xmlWriterSettings.OmitXmlDeclaration = settings.OmitXmlDeclaration;
            xmlWriterSettings.Indent = true;
            using (var xmlWriter = XmlWriter.Create(ms, xmlWriterSettings)) {
                Serialize(xmlWriter, instance);
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        public string SerializeToStringUTF8NoBom(object instance) {
            var ms = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = new System.Text.UTF8Encoding(false);
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = settings.OmitXmlDeclaration;
            using (var xmlWriter = XmlWriter.Create(ms, xmlWriterSettings)) {
                Serialize(xmlWriter, instance);
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        public void Serialize(TextWriter textWriter, object instance) {
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = new System.Text.UTF8Encoding(false);
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = settings.OmitXmlDeclaration;
            using (var xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings)) {
                Serialize(xmlWriter, instance);
            }
        }
        public void Serialize(XmlWriter xmlWriter, object instance) {
            xmlWriter.WriteStartDocument();
            SerializeInstanceRecursive(xmlWriter, instance, null, false);
            xmlWriter.WriteEndDocument();
        }
        private void SerializeInstanceRecursive(XmlWriter xmlWriter, object instance, string? nodeName, bool isArrayElement) {
            var type = instance.GetType();
            if (nodeName != null) {
                xmlWriter.WriteStartElement(nodeName);
            } else {
                if (instance is VO) {
                    xmlWriter.WriteStartElement("vo");
                } else {
                    var typeName = settings.ProcessName(instance.GetType().Name);
                    xmlWriter.WriteStartElement(typeName);
                }                
            }
            if (instance is VO) {
                var vo = (VO)instance;
                foreach (var key in vo.Keys) {
                    if (key == null) continue;
                    if (!key.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        var attributeName = (string)key;
                        var attributeValue = SerializeAttributeValue(vo[attributeName], true);
                        if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                    }
                }
                foreach (var key in vo.Keys) {
                    if (key.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        var content = vo[key];
                        if (content != null) xmlWriter.WriteString(content.ToString());
                    }
                }
            } else if (instance is IDictionary<string, object?>) {
                var vo = (IDictionary<string, object?>)instance;
                foreach (var key in vo.Keys) {
                    if (key == null) continue;
                    if (!key.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        var attributeName = (string)key;
                        var attributeValue = SerializeAttributeValue(vo[attributeName], true);
                        if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                    }
                }
                foreach (var key in vo.Keys) {
                    if (key.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        var content = vo[key];
                        if (content != null) xmlWriter.WriteString(content.ToString());
                    }
                }
            } else if (instance is IDictionary<string, string>) {
                var nameValueCollection = (IDictionary<string, string>)instance;
                foreach (var key in nameValueCollection.Keys) {
                    if (key == null) continue;
                    var attributeName = (string)key;
                    var attributeValue = SerializeAttributeValue(nameValueCollection[attributeName], true);
                    if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                }
            } else if (instance is NameValueCollection) {
                var nameValueCollection = (NameValueCollection)instance;
                foreach (var key in nameValueCollection.Keys) {
                    if (key == null) continue;
                    var attributeName = (string)key;
                    var attributeValue = SerializeAttributeValue(nameValueCollection[attributeName], false);
                    if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                }
            } else if (instance is StringDictionary) {
                var stringDictionary = (StringDictionary)instance;
                foreach (var key in stringDictionary.Keys) {
                    if (key == null) continue;
                    var attributeName = (string)key;
                    var attributeValue = SerializeAttributeValue(stringDictionary[attributeName], true);
                    if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                }
            } else {
                var propertyBindings = BindingFlags.Instance | BindingFlags.Public;
                var propertyInfos = new List<PropertyInfo>(type.GetProperties(propertyBindings));
                //attributes
                foreach (var propertyInfo in propertyInfos.ToArray()) {
                    var attributeName = settings.ProcessName(propertyInfo.Name);
                    if (!settings.IsSerializable(instance, propertyInfo)) {
                    } else if (propertyInfo.Name.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                    } else if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType.IsEnum || propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(Type) || propertyInfo.PropertyType == typeof(object)) {
                        var value = propertyInfo.GetValue(instance);
                        var attributeValue = SerializeAttributeValue(value, false);
                        if (attributeValue != null) xmlWriter.WriteAttributeString(attributeName, attributeValue);
                        propertyInfos.Remove(propertyInfo);
                    } else if (propertyInfo.PropertyType.IsArray) {
                        var elementType = propertyInfo.PropertyType.GetElementType();
                        if (elementType != null) {
                                if (elementType.IsValueType || elementType.IsEnum || elementType == typeof(string) || elementType == typeof(DateTime) || elementType == typeof(Type) || elementType == typeof(object)) {
                                var value = propertyInfo.GetValue(instance);
                                if (value != null) {
                                    var enumrable = (IEnumerable)value;
                                    var aux = new StringBuilder();
                                    foreach (var valueItem in (IEnumerable)value) {
                                        var attributeItem = SerializeAttributeValue(valueItem, true);
                                        if (attributeItem != null) {
                                            if (aux.Length > 0) aux.Append(",");
                                            aux.Append(attributeItem);
                                        }
                                    }
                                    if (settings.AvoidEmptyArrays && aux.Length == 0) {
                                    } else {
                                        xmlWriter.WriteAttributeString(attributeName, aux.ToString());
                                    }
                                }
                                propertyInfos.Remove(propertyInfo);
                            }
                        }
                    }
                }
                //arrays
                foreach (var propertyInfo in propertyInfos.ToArray()) {
                    if (!settings.IsSerializable(instance, propertyInfo)) {
                    } else if (propertyInfo.Name.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                    } else if (propertyInfo.PropertyType.IsArray) {
                        //array
                        var value = propertyInfo.GetValue(instance);
                        if (value != null) {
                            var valueArr = (object[])value;
                            if (settings.AvoidEmptyArrays && valueArr.Length == 0) {
                            } else {
                                xmlWriter.WriteStartElement(settings.ProcessName(propertyInfo.Name));
                                foreach (var item in valueArr) {
                                    if (item != null) SerializeInstanceRecursive(xmlWriter, item, null, true);
                                }
                                xmlWriter.WriteEndElement();
                            }
                        }
                        propertyInfos.Remove(propertyInfo);
                    } else if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                        //list<>
                        var value = propertyInfo.GetValue(instance);
                        if (value != null) {
                            var valueArr = (IList)value;
                            if (settings.AvoidEmptyArrays && valueArr.Count == 0) {
                            } else {
                                xmlWriter.WriteStartElement(settings.ProcessName(propertyInfo.Name));
                                foreach (var item in valueArr) {
                                    if (item != null) SerializeInstanceRecursive(xmlWriter, item, null, true);
                                }
                                xmlWriter.WriteEndElement();
                            }
                        }
                        propertyInfos.Remove(propertyInfo);
                    } else if (propertyInfo.PropertyType.BaseType != null && propertyInfo.PropertyType.BaseType.IsGenericType && propertyInfo.PropertyType.BaseType.GetGenericTypeDefinition() == typeof(List<>)) {
                        //extends list<>
                        var value = propertyInfo.GetValue(instance);
                        if (value != null) {
                            xmlWriter.WriteStartElement(settings.ProcessName(propertyInfo.Name));
                            foreach (var subPropertyInfo in value.GetType().GetProperties(propertyBindings)) {
                                if (subPropertyInfo.Name.Equals("Capacity")) {
                                } else if (subPropertyInfo.Name.Equals("Count")) {
                                } else if (subPropertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType.IsEnum || propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(Type)) {
                                    var subValue = subPropertyInfo.GetValue(value);
                                    var attributeSubName = settings.ProcessName(subPropertyInfo.Name);
                                    var attributeSubValue = SerializeAttributeValue(subValue, true);
                                    if (attributeSubValue != null) xmlWriter.WriteAttributeString(attributeSubName, attributeSubValue);
                                }
                            }
                            foreach (var item in (IList)value) {
                                if (item != null) SerializeInstanceRecursive(xmlWriter, item, null, true);
                            }
                            xmlWriter.WriteEndElement();
                        }
                        propertyInfos.Remove(propertyInfo);
                    } else if (propertyInfo.GetIndexParameters().Length > 0) {
                    } else {
                        //object
                        var value = propertyInfo.GetValue(instance);
                        if (value != null) {
                            SerializeInstanceRecursive(xmlWriter, value, settings.ProcessName(propertyInfo.Name), false);
                        }
                        propertyInfos.Remove(propertyInfo);
                    }
                }
                //text
                foreach (var propertyInfo in propertyInfos) {
                    if (propertyInfo.Name.Equals(settings.ContentPropertyName, StringComparison.OrdinalIgnoreCase)) {
                        var value = propertyInfo.GetValue(instance);
                        var textValue = SerializeAttributeValue(value, false);
                        if (textValue != null) {
                            xmlWriter.WriteStartElement("content");
                            if (textValue.IndexOf("<") != -1) {
                                xmlWriter.WriteCData(textValue);
                            } else {
                                xmlWriter.WriteString(textValue);
                            }
                            xmlWriter.WriteEndElement();
                        }
                    }
                }
            }
            xmlWriter.WriteEndElement();
        }


        //private methods
        private string? SerializeAttributeValue(object? value, bool allowAvoids) {
            if (value == null) {
                return null;
            } else if (value is string && settings.AvoidEmptyStrings && ((string)value).Length == 0 && !allowAvoids) {
                return null;
            } else if (value is bool && settings.AvoidFalseBooleans && !((bool)value) && !allowAvoids) {
                return null;
            } else if (value is short && settings.AvoidZeroNumbers && ((short)value) == 0 && !allowAvoids) {
                return null;
            } else if (value is int && settings.AvoidZeroNumbers && ((int)value) == 0 && !allowAvoids) {
                return null;
            } else if (value is long && settings.AvoidZeroNumbers && ((long)value) == 0 && !allowAvoids) {
                return null;
            } else if (value is float && settings.AvoidZeroNumbers && ((float)value) == 0.0 && !allowAvoids) {
                return null;
            } else if (value is double && settings.AvoidZeroNumbers && ((double)value) == 0.0 && !allowAvoids) {
                return null;
            } else if (value is decimal && settings.AvoidZeroNumbers && ((decimal)value) == 0.0m && !allowAvoids) {
                return null;
            } else if (value is Enum) {
                if (settings.AvoidDefaultEnumValues && !allowAvoids) {
                    var aux = value.ToString()!;
                    var names = System.Enum.GetNames(value.GetType());
                    if (names[0].Equals(aux)) return null;
                    value = aux;
                }
                return settings.ProcessName(value.ToString()!);
            } else if (value is byte[]) {
                return Convert.ToBase64String((byte[])value);
            }
            return ConvertUtils.To<string>(value);
        }

    }

}


