
using DProjects.Utils;
using System.Xml;

namespace DProjects.Fs.Extensions {


    public static class EntryToXml {


        //methods
        public static XmlElement ToXml(this Entry entry, XmlDocument xmlDocument, string? path = null) {
            if (path == null) path = entry.Path;
            XmlElement xmlElement = xmlDocument.CreateElement((entry.IsDirectory() ? "dir" : "file"));
            xmlElement.SetAttribute("path", path);
            xmlElement.SetAttribute("length", entry.Length.ToString());
            xmlElement.SetAttribute("created", entry.Created.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            xmlElement.SetAttribute("modified", entry.Modified.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            xmlElement.SetAttribute("etag", entry.Etag ?? "");
            xmlElement.SetAttribute("flags", entry.Flags.ToString());
            return xmlElement;
        }
        public static void ToXml(this Entry entry, XmlWriter xmlWriter, string? path = null, bool noWritePath = false, bool noEndElement = false) {
            if (path == null) path = entry.Path;
            xmlWriter.WriteStartElement((entry.IsDirectory() ? "dir" : "file"));
            if (!noWritePath) xmlWriter.WriteAttributeString("path", path);
            xmlWriter.WriteAttributeString("name", PathUtils.GetPathName(path));
            xmlWriter.WriteAttributeString("length", entry.Length.ToString());
            xmlWriter.WriteAttributeString("created", entry.Created.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            xmlWriter.WriteAttributeString("modified", entry.Modified.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            xmlWriter.WriteAttributeString("etag", entry.Etag ?? "");
            xmlWriter.WriteAttributeString("flags", entry.Flags.ToString());
            if (!noEndElement) xmlWriter.WriteEndElement();
        }


    }


}