using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace DProjects.Utils {


    public static class XmlUtils {


        //load
        public static XmlDocument LoadXml(string xml) {
            try {
                var result = new XmlDocument();
                result.LoadXml(xml);
                return result;
            } catch (Exception ex) {
                throw new ArgumentException("Error loading xml data: " + ex.Message, ex);
            }
        }
        public static XmlDocument LoadXml(TextReader reader) {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(reader);
            return xmlDocument;
        }
        public static XmlDocument LoadXml(Stream stream) {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);
            return xmlDocument;
        }
        public static XmlDocument LoadXmlFile(string path) {
            var xmlDocument = new XmlDocument();
            try {
                xmlDocument.Load(new MemoryStream(FileUtils.ReadFile(path)));
            } catch (FileNotFoundException e) {
                throw e;
            } catch (Exception e) {
                throw new Exception("Error loading xml file: " + e.Message, e);
            }
            return xmlDocument;
        }        
        public static void ValidateXmlDocumentAgainstSchema(XmlDocument xmlDocument, XmlSchema xmlSchema) {
            xmlDocument.Schemas.Add(xmlSchema);
            xmlDocument.Validate(null);
        }
        public static bool ValidateXmlDocumentAgainstSchema(XmlDocument xmlDocument, XmlSchema xmlSchema, out string[] errors) {
            var errorList = new List<string>();
            xmlDocument.Schemas.Add(xmlSchema);
            xmlDocument.Validate((object sender, System.Xml.Schema.ValidationEventArgs args) => {
                errorList.Add(args.Severity.ToString() + ": " + args.Message);
            });
            errors = errorList.ToArray();
            return errors.Length == 0;
        }
        public static string ToIndentedString(XmlDocument doc) {
            var encoding = Encoding.UTF8;
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;
            using (var ms = new MemoryStream()) {
                using (var writer = XmlWriter.Create(ms, settings)) {
                    //writer.WriteStartDocument();
                    //doc.Save(writer);
                    doc.WriteTo(writer);

                }
                return encoding.GetString(ms.ToArray());
            }
        }


        ////child nodes
        //public static XmlNode SetXmlChildNode(XmlNode xmlNode, string childNodeName, object value) {
        //    XmlNode? xmlChildNode = GetXmlChildNode(xmlNode, childNodeName);
        //    if (xmlChildNode == null) {
        //        xmlChildNode = xmlNode.OwnerDocument.CreateElement(childNodeName);
        //        xmlNode.AppendChild(xmlChildNode);
        //    }
        //    xmlChildNode.InnerText = ConvertUtils.To<string>(value);
        //    return xmlChildNode;
        //}
        //public static XmlNode? GetXmlChildNode(XmlNode xmlNode, string childNodeName) {
        //    foreach (XmlNode? aux in xmlNode.ChildNodes) {
        //        if (aux == null) break;
        //        if (aux.Name.Equals(childNodeName)) return aux;
        //    }
        //    return null;
        //}
        //public static T GetXmlChildNodeAs<T>(XmlNode xmlNode, string childNodeName, T defaultValue) {
        //    XmlNode? xmlChildNode = GetXmlChildNode(xmlNode, childNodeName);
        //    if (xmlChildNode != null) {
        //        return ConvertUtils.To<T>(xmlChildNode.InnerText);
        //    } else {
        //        return defaultValue;
        //    }
        //}


        ////attributes
        //public static void SetXmlAttribute(XmlNode xmlNode, string attributeName, object value) {
        //    XmlAttribute? xmlAttribute = xmlNode.Attributes[attributeName];
        //    if (xmlAttribute == null) {
        //        xmlAttribute = xmlNode.OwnerDocument.CreateAttribute(attributeName);
        //        xmlNode.Attributes.Append(xmlAttribute);
        //    }
        //    xmlAttribute.Value = ConvertUtils.To<string>(value);
        //}
        //public static void SetXmlAttribute(XmlNode xmlNode, string attributeName, object value, string namespaceUri) {
        //    XmlAttribute? xmlAttribute = xmlNode.Attributes[attributeName, namespaceUri];
        //    if (xmlAttribute == null) {
        //        xmlAttribute = xmlNode.OwnerDocument.CreateAttribute(attributeName, namespaceUri);
        //        xmlNode.Attributes.Append(xmlAttribute);
        //    }
        //    xmlAttribute.Value = ConvertUtils.To<string>(value);
        //}
        //public static T GetXmlAttributeAs<T>(XmlNode? xmlNode, string attributeName, T defaultValue) {
        //    if (xmlNode == null || xmlNode.Attributes.GetNamedItem(attributeName) == null) {
        //        return defaultValue;
        //    } else {
        //        return ConvertUtils.To<T>(xmlNode.Attributes.GetNamedItem(attributeName).Value);
        //    }
        //}


        //scan for variables like ..${var1}... in attributes value
        public static void ScanXmlAttributesForVariables(XmlNode xmlNode, Func<string, string?> callback) { 
            if (xmlNode.Attributes != null) {
                foreach (XmlAttribute xmlAttribute in xmlNode.Attributes) {
                    var i = xmlAttribute.Value.IndexOf("${");
                    if (i != -1) {
                        var value = xmlAttribute.Value;
                        do {
                            try {
                                int j = value.IndexOf("}", i);
                                if (j == -1) break;
                                var key = value.Substring(i + 2, j - i - 2);
                                var replacement = callback(key);
                                if (replacement != null) {
                                    value = value.Substring(0, i) + replacement + value.Substring(j + 1);
                                    i = value.IndexOf("${", i + replacement.Length);
                                } else {
                                    i = value.IndexOf("${", i + 1);
                                }
                            } catch (Exception e) {
                                throw new Exception("Error parsing: " + xmlAttribute.Value, e);
                            }
                        } while (i != -1);
                        xmlAttribute.Value = value;
                    }
                }
            }
            if (xmlNode.ChildNodes != null) {
                foreach (XmlNode xmlChild in xmlNode.ChildNodes) {
                    if (xmlChild != null) {
                        ScanXmlAttributesForVariables(xmlChild, callback);
                    }
                }
            }
        }



        ////xml attributes
        //public static NameValueCollection GetXmlAttributesAsNameValueCollection(string text) {
        //    //ex: tags="aaaa,bbb" value1="asd" value2="234234"
        //    var result = new StringDictionary();
        //    if (text.StartsWith(" ") || text.EndsWith(" ")) text = text.Trim();
        //    var queryString = new StringBuilder(text.Length);
        //    var insideQuotes = false;
        //    for (var i = 0; i < text.Length; i++) {
        //        var c = text[i];
        //        if (c == '"') {
        //            insideQuotes = !insideQuotes;
        //        } else if (c == ' ' && !insideQuotes) {
        //            c = '&';
        //            queryString.Append(c);
        //        } else if (c == '&' && !insideQuotes) {
        //            queryString.Append("&amp;");
        //        } else {
        //            queryString.Append(c);
        //        }
        //    }
        //    return HttpUtility.ParseQueryString(queryString.ToString());
        //}
        //public static IDictionary<string, object?> GetXmlAttributesAsDictionary(string text) {
        //    //ex: tags="aaaa,bbb" value1="asd" value2="234234"
        //    text = text.Trim();
        //    var result = new Dictionary<string, object?>();
        //    if (text.Length > 0) {
        //        var aux = text.Split('"');
        //        for (var i = 0; i < aux.Length; i += 2) {
        //            var key = aux[i].Trim();
        //            key = key.Substring(key.LastIndexOfAny(new char[] { ' ', '\t', '\r', '\n' }) + 1);
        //            if (key.EndsWith("=")) {
        //                key = key.Substring(0, key.Length - 1);
        //                var value = (i < aux.Length - 1 ? aux[i + 1].Trim() : "");
        //                result[key] = value;
        //            }
        //        }
        //    }
        //    return result;
        //}



    }

}


