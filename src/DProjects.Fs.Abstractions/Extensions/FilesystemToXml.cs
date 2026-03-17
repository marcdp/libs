
using DProjects.Utils;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Fs.Extensions {


    public static class FilesystemToXml {

        //inner classes
        public class ToXmlSettings {
            public string? Pattern { get; set; } = "*";
            public bool Content { get; set; } = default!;
            public int ContentBlockSize { get; set; } = 78;
            public bool Metadata { get; set; } = default!;
            public bool Report { get; set; } = default!;
            public bool Base64Content { get; set; } = default!;
        }

        //methods
        private static async Task ToXmlRecursiveAsync(IFilesystem fs, Entry entry, XmlWriter xmlWriter, ToXmlSettings settings, int level, Stats stats, CancellationToken cancellationToken) {
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
                stats.Directories++;
                await foreach (var childEntry in fs.GetEntriesAsync(entry.Path, GetModes.All, settings.Pattern, cancellationToken)) {
                    await ToXmlRecursiveAsync (fs, childEntry, xmlWriter, settings, level + 1, stats, cancellationToken);
                }
            } else if (entry.IsFile()) {
                if (settings.Content) {
                    if (!settings.Base64Content && MimeTypeUtils.IsText(MimeTypeUtils.GetMimeType(entry.Path))) {
                        var buffer = new char[settings.ContentBlockSize];
                        using (var stream = await fs.LoadReadStreamAsync(entry.Path, new(), cancellationToken)) 
                        using (var reader = new StreamReader(stream)) { 
                            var chunks = 0;
                            do {
                                var i = 0;
                                do {
                                    var l = buffer.Length - i;
                                    var nRead = await reader.ReadBlockAsync(buffer, i, l);
                                    if (nRead == 0) break;
                                    i += nRead;
                                } while (i < buffer.Length);
                                if (i == 0) break;
                                xmlWriter.WriteStartElement("content");
                                xmlWriter.WriteString(new string(buffer, 0, i));
                                xmlWriter.WriteEndElement();
                                chunks++;
                            } while (true);
                            if (chunks == 0) {
                                xmlWriter.WriteStartElement("content");
                                xmlWriter.WriteEndElement();
                            }
                        }
                    } else {
                        var buffer = new byte[settings.ContentBlockSize];
                        using (var stream = await fs.LoadReadStreamAsync(entry.Path, new(), cancellationToken)) {
                            var chunks = 0;
                            do {
                                var i = 0;
                                do {
                                    var l = buffer.Length - i;
                                    var nRead = await stream.ReadAsync(buffer, i, l, cancellationToken);
                                    if (nRead == 0) break;
                                    i += nRead;
                                } while (i < buffer.Length);
                                if (i == 0) break;
                                xmlWriter.WriteStartElement("content");
                                xmlWriter.WriteAttributeString("encoding", "base64");
                                xmlWriter.WriteBase64(buffer, 0, i);
                                xmlWriter.WriteEndElement();
                                chunks++;
                            } while (true);
                            if (chunks == 0) {
                                xmlWriter.WriteStartElement("content");
                                xmlWriter.WriteAttributeString("encoding", "base64");
                                xmlWriter.WriteEndElement();
                            }
                        }
                    }
                }
                stats.Files++;
                stats.Length += entry.Length;
            }
            if (level == 0 && settings.Report) {
                xmlWriter.WriteStartElement("report");
                xmlWriter.WriteAttributeString("directories", stats.Directories.ToString());
                xmlWriter.WriteAttributeString("files", stats.Files.ToString());
                xmlWriter.WriteAttributeString("length", stats.Length.ToString());
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }
        public static async Task ToXmlAsync(this IFilesystem fs, string path, XmlWriter xmlWriter, ToXmlSettings settings, CancellationToken cancellationToken) {
            var stats = new Stats();
            var entry = await fs.GetEntryAsync(path, cancellationToken);
            if (entry != null) {
                await ToXmlRecursiveAsync(fs, entry, xmlWriter, settings, 0, stats, cancellationToken);
            }
        }
        private class Stats {
            public int Directories { get; set; }
            public int Files { get; set; }
            public long Length { get; set; }
        }

    }

}
