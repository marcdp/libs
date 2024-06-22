
using DProjects.Utils;
using System.Xml;

namespace DProjects.Fs.Extensions {


    public static class FilesystemToXml {

        //inner classes
        public class ToXmlSettings {
            public string? Pattern { get; set; } = "*";
            public bool Content { get; set; } = default!;
            public bool Metadata { get; set; } = default!;
            public bool Report { get; set; } = default!;
            public bool Base64Content { get; set; } = default!;
        }

        //methods
        private static void ToXmlRecursive(IFilesystem fs, Entry entry, XmlWriter xmlWriter, ToXmlSettings settings, int level, ref long directories, ref long files, ref long length) {
            entry.ToXml(xmlWriter, noWritePath: true, noEndElement: true);
            if (settings.Metadata) {
                if (fs.Supports(entry.Path, Features.Metadata)) {
                    var metadata = fs.GetMetadata(entry.Path);
                    if (metadata.Count > 0) {
                        xmlWriter.WriteStartElement("meta");
                        foreach (var pair in metadata) {
                            xmlWriter.WriteStartElement("variable");
                            xmlWriter.WriteAttributeString("key", pair.Key);
                            xmlWriter.WriteAttributeString("value", pair.Value);
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                    }
                }
            }
            if (entry.IsDirectory()) {
                directories++;
                foreach (var childEntry in fs.GetEntries(entry.Path, GetModes.All, settings.Pattern)) {
                    ToXmlRecursive(fs, childEntry, xmlWriter, settings, level + 1, ref directories, ref files, ref length);
                }
            } else if (entry.IsFile()) {
                if (settings.Content) {
                    xmlWriter.WriteStartElement("content");
                    if (!settings.Base64Content && MimeTypeUtils.IsText(MimeTypeUtils.GetMimeType(entry.Path))) {
                        var text = fs.LoadTextFile(entry.Path);
                        xmlWriter.WriteString(text);
                    } else {
                        xmlWriter.WriteAttributeString("encoding", "base64");
                        var buffer = new byte[60];
                        using (var stream = fs.LoadReadStream(entry.Path, new())) {
                            do {
                                var i = stream.Read(buffer, 0, buffer.Length);
                                if (i == 0) break;
                                xmlWriter.WriteBase64(buffer, 0, i);
                            } while (true);
                        }
                    }
                    xmlWriter.WriteEndElement();
                }
                files++;
                length += entry.Length;
            }
            if (level == 0 && settings.Report) {
                xmlWriter.WriteStartElement("report");
                xmlWriter.WriteAttributeString("directories", directories.ToString());
                xmlWriter.WriteAttributeString("files", files.ToString());
                xmlWriter.WriteAttributeString("length", length.ToString());
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }
        public static void ToXml(this IFilesystem fs, string path, XmlWriter xmlWriter, ToXmlSettings? settings = null) {
            if (settings == null) settings = new ToXmlSettings();
            long directories = 0;
            long files = 0;
            long length = 0;
            var entry = fs.GetEntry(path);
            if (entry != null) {
                ToXmlRecursive(fs, entry, xmlWriter, settings, 0, ref directories, ref files, ref length);
            }
        }

    }

}