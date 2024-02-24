
using DProjects.Utils;
using System.Collections.Generic;
using System.Text;

namespace DProjects.Fs.Extensions {


    public static class EntryToYml {


        //methods
        public static string ToYml(this Entry entry, string? path = null, IDictionary<string, string>? metadata = null, bool noEndElement = false) {
            if (path == null) path = entry.Path;
            var result = new StringBuilder();
            result.AppendLine("---");
            if (entry.IsFile()) {
                result.AppendLine("type: file");
            } else if (entry.IsDirectory()) {
                result.AppendLine("type: dir");
            }
            result.AppendLine("path: " + path);
            result.AppendLine("name: " + entry.Name);
            result.AppendLine("created: " + entry.Created.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            result.AppendLine("modified: " + entry.Modified.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            if (entry.IsFile()) result.AppendLine("length: " + entry.Length);
            if (!string.IsNullOrEmpty(entry.Etag)) result.AppendLine("etag: " + entry.Etag);
            if (entry.Flags != 0) result.AppendLine("flags: " + entry.Flags);
            if (metadata != null) {
                result.AppendLine("meta: ");
                foreach (var item in metadata) {
                    result.AppendLine(item.Key + ": " + item.Value);
                }
            }
            if (!noEndElement) result.AppendLine("---");
            return result.ToString();
        }


    }


}