using System;
using System.Collections.Generic;
using System.IO;
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
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(reader);
            return xmlDocument;
        }
        public static XmlDocument LoadXml(Stream stream) {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);
            return xmlDocument;
        }
        public static XmlDocument LoadXmlFile(string xmlPath) {
            XmlDocument xmlDocument = new XmlDocument();
            try {
                xmlDocument.Load(new MemoryStream(FileUtils.ReadFile(xmlPath)));
            } catch (FileNotFoundException e) {
                throw e;
            } catch (Exception e) {
                throw new Exception("Error loading xml file \'" + xmlPath + "\': " + e.Message, e);
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
        public static string ToIndentedString(XmlDocument xmlDoc) {
            var ms = new MemoryStream();
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = System.Text.Encoding.UTF8;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "    ";
            using (var xmlWriter = System.Xml.XmlWriter.Create(new StreamWriter(ms, System.Text.Encoding.UTF8), xmlWriterSettings)) {
                xmlDoc.WriteTo(xmlWriter);
            }
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }


        //child nodes
        public static XmlNode SetXmlChildNode(XmlNode xmlNode, string childNodeName, object value) {
            XmlNode? xmlChildNode = GetXmlChildNode(xmlNode, childNodeName);
            if (xmlChildNode == null) {
                xmlChildNode = xmlNode.OwnerDocument.CreateElement(childNodeName);
                xmlNode.AppendChild(xmlChildNode);
            }
            xmlChildNode.InnerText = ConvertUtils.To<string>(value);
            return xmlChildNode;
        }
        public static XmlNode? GetXmlChildNode(XmlNode xmlNode, string childNodeName) {
            foreach (XmlNode? aux in xmlNode.ChildNodes) {
                if (aux == null) break;
                if (aux.Name.Equals(childNodeName)) return aux;
            }
            return null;
        }
        public static T GetXmlChildNodeAs<T>(XmlNode xmlNode, string childNodeName, T defaultValue) {
            XmlNode? xmlChildNode = GetXmlChildNode(xmlNode, childNodeName);
            if (xmlChildNode != null) {
                return ConvertUtils.To<T>(xmlChildNode.InnerText);
            } else {
                return defaultValue;
            }
        }


        //attributes
        public static void SetXmlAttribute(XmlNode xmlNode, string attributeName, object value) {
            XmlAttribute? xmlAttribute = xmlNode.Attributes[attributeName];
            if (xmlAttribute == null) {
                xmlAttribute = xmlNode.OwnerDocument.CreateAttribute(attributeName);
                xmlNode.Attributes.Append(xmlAttribute);
            }
            xmlAttribute.Value = ConvertUtils.To<string>(value);
        }
        public static void SetXmlAttribute(XmlNode xmlNode, string attributeName, object value, string namespaceUri) {
            XmlAttribute? xmlAttribute = xmlNode.Attributes[attributeName, namespaceUri];
            if (xmlAttribute == null) {
                xmlAttribute = xmlNode.OwnerDocument.CreateAttribute(attributeName, namespaceUri);
                xmlNode.Attributes.Append(xmlAttribute);
            }
            xmlAttribute.Value = ConvertUtils.To<string>(value);
        }
        public static T GetXmlAttributeAs<T>(XmlNode? xmlNode, string attributeName, T defaultValue) {
            if (xmlNode == null || xmlNode.Attributes.GetNamedItem(attributeName) == null) {
                return defaultValue;
            } else {
                return ConvertUtils.To<T>(xmlNode.Attributes.GetNamedItem(attributeName).Value);
            }
        }


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

    }

}


