using DProjects.Utils;
using System;
using System.IO;
using System.Text.Json;
using System.Text;

namespace DProjects.Fs {
    
    
    public record EntryDTO(string Path,
                           EntryType Type,
                           DateTime Created,
                           DateTime Modified,
                           long Length,
                           string Etag,
                           int Flags) {
        public Entry ToEntry(string? pathPrefix = null, string? pathBase = null) {
            var path = Path;
            if (pathPrefix != null && !String.IsNullOrEmpty(pathPrefix)) {
                path = PathUtils.Uncombine(pathBase?.Substring(pathPrefix.Length) ?? "", path);
            }
            return new Entry(path, Type, Created, Modified, Length, Etag, Flags);
        }
        public static EntryDTO FromEntry(Entry entry) {
            return new(entry.Path, entry.EntryType, entry.Created, entry.Modified, entry.Length, entry.Etag, entry.Flags);
        }
        public static EntryDTO FromJson(string json) {
            return JsonSerializer.Deserialize<EntryDTO>(json, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true,
            })!;
        }
        public string ToJson(string? path = null, bool writePath = true, bool writeName = false, bool noEndElement = false) {
            var result = new StringBuilder();
            if (path == null) path = Path;
            result.Append("{");
            if (writePath) result.Append("\"path\":").Append(JsonSerializer.Serialize(path));
            if (writeName) result.Append(result.Length > 1 ? "," : "").Append("\"name\":").Append(JsonSerializer.Serialize(PathUtils.GetPathName(path)));
            result.Append(result.Length > 1 ? "," : "").Append("\"length\":").Append(Length);
            result.Append(",\"created\":\"").Append(Created.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS)).Append("\"");
            result.Append(",\"modified\":\"").Append(Modified.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS)).Append("\"");
            result.Append(",\"type\":\"").Append(Type == EntryType.Directory ? "directory" : "file").Append("\"");
            result.Append(",\"etag\":").Append(JsonSerializer.Serialize(Etag));
            result.Append(",\"flags\":").Append(Flags);
            if (!noEndElement) result.Append("}");
            return result.ToString();
        }
    }


}

