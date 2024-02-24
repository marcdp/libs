
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using DProjects.Utils;

namespace DProjects.Fs.Extensions {


    public static class FilesystemToJson {


        //inner classes
        public class ToJsonSettings {
            public string? Pattern { get; set; } = "*";
            public bool Report { get; set; } = default!;
            public bool Content { get; set; } = default!;
            public bool Metadata { get; set; } = default!;
            public bool Base64Content { get; set; } = default!;
        }

        //methods
        private static void ToJsonRecursive(IFilesystem fs, Entry entry, TextWriter textWriter, ToJsonSettings settings, int level, ref long directories, ref long files, ref long length) {
            var json = entry.ToJson(writeName: true, writePath: false, noEndElement: true);
            textWriter.Write(json);
            if (settings.Metadata) {
                if (fs.Supports(entry.Path, Features.Metadata)) {
                    var metadata = fs.GetMetadata(entry.Path);
                    if (metadata.Count > 0) {
                        textWriter.Write(",\"meta\":[");
                        var i = 0;
                        foreach (var pair in metadata) {
                            if (i > 0) textWriter.Write(",");
                            textWriter.Write(JsonSerializer.Serialize(pair.Key));
                            textWriter.Write(":");
                            textWriter.Write(JsonSerializer.Serialize(pair.Value));
                            i++;
                        }
                        textWriter.Write("]");
                    }
                }
            }
            if (entry.IsDirectory()) {
                directories++;
                textWriter.Write(",\"childs\":[");
                var i = 0;
                foreach (var childEntry in fs.GetEntries(entry.Path, GetModes.All, settings.Pattern)) {
                    if (i > 0) textWriter.Write(",");
                    ToJsonRecursive(fs, childEntry, textWriter, settings, level + 1, ref directories, ref files, ref length);
                    i++;
                }
                textWriter.Write("]");
            } else if (entry.IsFile()) {
                if (settings.Content) {
                    textWriter.Write(",\"content\":{");
                    if (!settings.Base64Content && MimeTypeUtils.IsText(MimeTypeUtils.GetMimeType(entry.Path))) {
                        textWriter.Write("\"value\":");
                        var text = fs.LoadTextFile(entry.Path);
                        textWriter.Write(JsonSerializer.Serialize(text));
                    } else {
                        textWriter.Write("\"encoding\":\"base64\"");
                        textWriter.Write(",\"value\":");
                        textWriter.Write("\"");
                        var buffer = new byte[60];
                        using (var stream = fs.LoadReadStream(entry.Path)) {
                            do {
                                var j = stream.Read(buffer, 0, buffer.Length);
                                if (j == 0) break;
                                textWriter.Write(Base64Utils.ToBase64(buffer, 0, j));
                            } while (true);
                        }
                        textWriter.Write("\"");
                    }
                    textWriter.Write("}");
                }
                files++;
                length += entry.Length;
            }
            if (level == 0 && settings.Report) {
                textWriter.Write(",\"report\":");
                var report = new Dictionary<string, object>() {
                    { "directories",directories},
                    { "files",files},
                    { "length",length}
                };
                textWriter.Write(JsonSerializer.Serialize(report));
            }
            textWriter.Write("}");
        }
        public static void ToJson(this IFilesystem fs, string path, TextWriter textWriter, ToJsonSettings? settings = null) {
            if (settings == null) settings = new ToJsonSettings();
            long directories = 0;
            long files = 0;
            long length = 0;
            var entry = fs.GetEntry(path);
            if (entry != null) {
                ToJsonRecursive(fs, entry, textWriter, settings, 0, ref directories, ref files, ref length);
            }
        }

    }

}