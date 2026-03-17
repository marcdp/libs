using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;

namespace DProjects.Text.Xml {


    public class XmlDeserializer(XmlDeserializerSettings settings) : DProjects.Serialization.IDeserializer {

        //settings
        public class ChildsAttribute : System.Attribute {
            public string Name { get; set; }
            public ChildsAttribute(string name) {
                Name = name;
            }
        }

        //methods
        public T Deserialize<T>(Stream stream, Encoding encoding) {
            using var reader = new StreamReader(stream, encoding, false, 1024, true);
            return Deserialize<T>(reader);
        }
        public T Deserialize<T>(string xml) {            
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            return (T)Deserialize(typeof(T), xmlDocument);
        }
        public T Deserialize<T>(TextReader textReader) {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(textReader.ReadToEnd());
            return (T)Deserialize(typeof(T), xmlDocument);
        }
        public T Deserialize<T>(XmlDocument xmlDocument) {
            return (T)Deserialize(typeof(T), xmlDocument);
        }
        public object Deserialize(Type type, XmlDocument xmlDocument) {
            var instance = DeserializeRecursive(type, xmlDocument.DocumentElement, "/" + xmlDocument.DocumentElement.Name);
            return instance;
        }

        //private methods
        private object DeserializeRecursive(Type type, XmlElement xmlElement, string xpath) {
            if (!type.Name.Equals(settings.ProcessTypeName(xmlElement.Name))) {
                //throw new Exception("Unable to deserialize type: type not found: " + settings.ProcessTypeName(xmlElement.Name));
            }
            //value type
            if (type.IsValueType) {
                return ConvertUtils.To(xmlElement.InnerText, type, true)!;
            }
            //construct
            var constructors = type.GetConstructors();
            if (constructors.Length == 0) throw new Exception("Unable to deserialize type: no constructors found: " + type.FullName);
            var parameters = new List<object?>();
            var attributesNameConsumed = new List<string>();
            foreach (var parameterInfo in constructors[0].GetParameters()) {
                if (parameterInfo != null && parameterInfo.Name != null) {
                    attributesNameConsumed.Add(parameterInfo.Name);
                    var parameterName = xmlElement.GetAttribute(parameterInfo.Name);
                    var parameterType = parameterInfo.ParameterType;
                    var parameter = ConvertUtils.To(parameterName, parameterType, true);
                    parameters.Add(parameter);
                }
            }
            var instance = Activator.CreateInstance(type, parameters.ToArray()) ?? new object();
            if (instance is StringDictionary) {
                //StringDictionary
                var dictionary = (System.Collections.Specialized.StringDictionary)instance;
                foreach (XmlAttribute? xmlAttribute in xmlElement.Attributes) {
                    if (xmlAttribute != null && !attributesNameConsumed.Contains(xmlAttribute.Name)) {
                        dictionary.Add(xmlAttribute.Name, xmlAttribute.Value);
                    }
                }
            } else if (instance is NameValueCollection) {
                //NameValueCollection
                var nameValueCollection = (NameValueCollection)instance;
                foreach (XmlAttribute? xmlAttribute in xmlElement.Attributes) {
                    if (xmlAttribute != null && !attributesNameConsumed.Contains(xmlAttribute.Name)) {
                        nameValueCollection.Add(xmlAttribute.Name, xmlAttribute.Value);
                    }
                }
            } else {
                //object
                DeserializeInstanceProperties(instance, type, xmlElement, attributesNameConsumed, xpath);
            }
            return instance;
        }
        private void DeserializeInstanceProperties(object instance, Type type, XmlElement xmlElement, List<string> attributesNameConsumed, string xpath) {
            //if (typeof(IDictionary<string, object>).IsAssignableFrom(type)) {
            //    var json = xmlElement.InnerText.Trim();
            //    var dict = (IDictionary<string, object>)instance;
            //    var voAux = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
            //    foreach (var key in voAux.Keys) {
            //        vo[key] = voAux[key];
            //    }
            //    return;
            //} 
            foreach (XmlAttribute? xmlAttribute in xmlElement.Attributes) {
                if (xmlAttribute != null && !attributesNameConsumed.Contains(xmlAttribute.Name)) {
                    var propertyName = settings.ProcessPropertyName(xmlAttribute.Name);
                    if (settings.IgnoreNamespaces && xmlAttribute.Name.IndexOf(':') != -1) propertyName = settings.ProcessPropertyName(xmlAttribute.Name.Split(':')[1]);
                    var propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo == null) {
                        if (!settings.RequireAllProperties) continue;
                        throw new Exception("Unable to deserialize: property not found: " + xpath + "/@" + xmlAttribute.Name);
                    }
                    var propertyValue = ConvertUtils.To(xmlAttribute.Value, propertyInfo.PropertyType, true);
                    propertyInfo.SetValue(instance, propertyValue);
                }
            }
            foreach (var xmlChildNode in xmlElement.ChildNodes) {
                var xmlChild = xmlChildNode as XmlElement;
                if (xmlChild != null) {
                    var propertyName = settings.ProcessPropertyName(xmlChild.Name);
                    if (settings.IgnoreNamespaces && xmlChild.Name.IndexOf(':') != -1) propertyName = settings.ProcessPropertyName(xmlChild.Name.Split(':')[1]);
                    var propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo == null) {
                        if (!settings.RequireAllProperties) continue;
                        throw new Exception("Unable to deserialize: property not found: " + xpath + "/" + xmlChild.Name);
                    }
                    if (propertyInfo.Name.Equals(settings.ContentPropertyName)) {
                        //content
                        if (propertyInfo.PropertyType == typeof(byte[])) {
                            propertyInfo.SetValue(instance, Convert.FromBase64String(xmlChild.InnerText));
                        } else {
                            propertyInfo.SetValue(instance, xmlChild.InnerText);
                        }
                    } else if (propertyInfo.PropertyType.IsArray) {
                        //array
                        var arrayType = propertyInfo.PropertyType.GetElementType();
                        if (arrayType != null) {
                            var list = new List<object>();
                            foreach (var xmlArrayItemChild in xmlChild.ChildNodes) {
                                var xmlArrayItem = xmlArrayItemChild as XmlElement;
                                if (xmlArrayItem != null) {
                                    var arrayItem = DeserializeRecursive(arrayType, xmlArrayItem, xpath + "/" + xmlChild.Name + "/" + xmlArrayItem.Name + "[" + list.Count + "]");
                                    list.Add(arrayItem);
                                }
                            }
                            object[] realArray = (object[])Activator.CreateInstance(propertyInfo.PropertyType, new object[] { list.Count })!;
                            for (var i = 0; i < list.Count; i++) realArray[i] = list[i];
                            if (!propertyInfo.CanWrite) throw new Exception("Unable to set array: property is not writable: " + type.FullName + ": " + propertyInfo.Name);
                            propertyInfo.SetValue(instance, realArray);
                        }
                    } else if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                        //list<T>
                        var listItemType = propertyInfo.PropertyType.GetGenericArguments()[0];
                        var list = (IList)propertyInfo.GetValue(instance)!;
                        if (list == null) {
                            if (propertyInfo.CanWrite) {
                                list = (Activator.CreateInstance(propertyInfo.PropertyType) as IList)!;
                                propertyInfo.SetValue(instance, list);
                            } else {
                                throw new Exception("Unable to set list value: list is null and not writable: " + type.FullName + ": " + propertyInfo.Name);
                            }
                        }
                        foreach (XmlNode? xmlArrayItem in xmlChild.ChildNodes) {
                            if (xmlArrayItem != null && (xmlArrayItem as XmlElement) != null) {
                                var arrayItem = DeserializeRecursive(listItemType, (XmlElement)xmlArrayItem, xpath + "/" + xmlChild.Name + "/" + xmlArrayItem.Name + "[" + list.Count + "]");
                                list.Add(arrayItem);
                            }
                        }
                    } else if (propertyInfo.PropertyType.BaseType != null && propertyInfo.PropertyType.BaseType.IsGenericType && propertyInfo.PropertyType.BaseType.GetGenericTypeDefinition() == typeof(List<>)) {
                        //extends list<T>
                        var listItemType = propertyInfo.PropertyType.BaseType.GetGenericArguments()[0];
                        var list = (IList)propertyInfo.GetValue(instance)!;
                        if (list == null) {
                            if (propertyInfo.CanWrite) {
                                list = (Activator.CreateInstance(propertyInfo.PropertyType) as IList)!;
                                propertyInfo.SetValue(instance, list);
                            } else {
                                throw new Exception("Unable to set list value: list is null and not writable: " + type.FullName + ": " + propertyInfo.Name);
                            }
                        }
                        foreach (XmlNode? xmlArrayItem in xmlChild.ChildNodes) {
                            if (xmlArrayItem != null && (xmlArrayItem as XmlElement) != null) {
                                var arrayItem = DeserializeRecursive(listItemType, (XmlElement)xmlArrayItem, xpath + "/" + xmlChild.Name + "/" + xmlArrayItem.Name + "[" + list.Count + "]");
                                list.Add(arrayItem);
                            }
                        }
                    } else if (propertyInfo.PropertyType == (typeof(IDictionary<string, string>))) {
                        //IDictionary<string, string>
                        var dictionary = (IDictionary<string, string>)propertyInfo.GetValue(instance)!;
                        if (dictionary == null) {
                            if (propertyInfo.CanWrite) {
                                dictionary = new Dictionary<string, string>();
                                propertyInfo.SetValue(instance, dictionary);
                            } else {
                                throw new Exception("Unable to set dictionary value: dictionary is null and not writable: " + type.FullName + ": " + propertyInfo.Name);
                            }
                        }
                        foreach (XmlNode xmlAttribute in xmlChild.Attributes) {
                            dictionary[xmlAttribute.Name] = xmlAttribute.Value;
                        }
                    } else {
                        //object
                        var subInstance = propertyInfo.GetValue(instance);
                        if (subInstance == null || propertyInfo.CanWrite) {
                            if (propertyInfo.PropertyType == typeof(string)) {
                                var aux = xmlChild.InnerText.Trim();
                                propertyInfo.SetValue(instance, aux);
                            } else {
                                subInstance = DeserializeRecursive(propertyInfo.PropertyType, xmlChild, xpath + "/" + xmlChild.Name);
                                propertyInfo.SetValue(instance, subInstance);
                            }
                        } else {
                            DeserializeInstanceProperties(subInstance, propertyInfo.PropertyType, xmlChild, new List<string>(), xpath + "/" + xmlChild.Name);
                        }
                    }
                }
            }
            //child
            foreach (var propertyInfo in type.GetProperties()) {
                var attribute = propertyInfo.GetCustomAttribute<ChildsAttribute>();
                if (attribute != null) {
                    if (propertyInfo.PropertyType.IsArray) {
                        var arrayType = propertyInfo.PropertyType.GetElementType();
                        if (arrayType != null) {
                            var list = new List<object>();
                            foreach (var xmlChildNode in xmlElement.ChildNodes) {
                                var xmlArrayItem = xmlChildNode as XmlElement;
                                if (xmlArrayItem != null && xmlArrayItem.Name == attribute.Name) {
                                    var arrayItem = DeserializeRecursive(arrayType, xmlArrayItem, xpath + "/" + xmlArrayItem.Name + "/" + xmlArrayItem.Name + "[" + list.Count + "]");
                                    list.Add(arrayItem);
                                }
                            }
                            object[] realArray = (object[])Activator.CreateInstance(propertyInfo.PropertyType, new object[] { list.Count })!;
                            for (var i = 0; i < list.Count; i++) realArray[i] = list[i];
                            if (!propertyInfo.CanWrite) throw new Exception("Unable to set array: property is not writable: " + type.FullName + ": " + propertyInfo.Name);
                            propertyInfo.SetValue(instance, realArray);
                        }
                    }
                }
            }
        }
    }

}


