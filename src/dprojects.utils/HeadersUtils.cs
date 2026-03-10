using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class HeadersUtils {


        //class
        public class Headers : IEnumerable<KeyValuePair<string,string>>   {
            private IDictionary<string, string> mItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public bool Contains(string name) {
                return mItems.ContainsKey(name);
            }
            public T Get<T>(string name, T defaultValue) {
                if (!Contains(name)) return defaultValue;
                var result = new StringBuilder();
                if (mItems.TryGetValue(name, out string value)) return ConvertUtils.To<T>(value);
                return defaultValue;
            }
            public void Set<T>(string name, T value) {
                if (value == null) return;
                if (this.Contains(name)) Remove(name);
                if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?)) {
                    mItems.Add(name, ((DateTime)(object)value).ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601).Replace('.', ':'));
                } else {
                    mItems.Add(name, value.ToString());
                }   
            }
            public void Remove(string name) {
                mItems.Remove(name);
            }
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
                return mItems.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return mItems.GetEnumerator();
            }
        }


        // HttpHeaders
        public static Headers ReadHttpHeaders(Stream stream, Encoding? encoding = null) {
            //if (encoding == null) encoding = EncodingUtils.GetDefault();
            if (encoding == null) encoding = new UTF8Encoding(false);
            var result = new Headers();
            do {
                var line = StreamUtils.ReadLine(stream, encoding);
                if (line == null || line.Length == 0) break;
                var i = line.IndexOf(":");
                if (i != -1) {
                    var name = line.Substring(0, i);
                    var value = line.Substring(i + 1).Trim();
                    result.Set(name, value);
                }
            } while (true);
            return result;
        }
        public static void WriteHttpHeaders(Headers headers, Stream stream, Encoding? encoding = null) {
            if (encoding == null) encoding = new UTF8Encoding(false);
            using (var streamWriter = new StreamWriter(stream, encoding, 1024, true)) {
                foreach (var header in headers) {
                    var name = header.Key;
                    var value = header.Value;
                    var line = name + ": " + value;
                    streamWriter.WriteLine(line);
                }
                streamWriter.WriteLine();
            }
        }
        public static async Task<Headers> ReadHttpHeadersAsync(Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
            if (encoding == null) encoding = new UTF8Encoding(false);
            var result = new Headers();
            do {
                var line = await StreamUtils.ReadLineAsync(stream, encoding, cancellationToken);
                if (line == null || line.Length == 0) break;
                var i = line.IndexOf(":");
                if (i != -1) {
                    var name = line.Substring(0, i);
                    var value = line.Substring(i + 1).Trim();
                    result.Set(name, value);
                }
            } while (true);
            return result;
        }
        public static async Task WriteHttpHeadersAsync(Headers headers, Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default) {
            if (encoding == null) encoding = new UTF8Encoding(false);
            using (var streamWriter = new StreamWriter(stream, encoding, 1024, true)) {
                foreach (var header in headers) {
                    var name = header.Key;
                    var value = header.Value;
                    var line = name + ": " + value;
                    await streamWriter.WriteLineAsync(line);
                }
                await streamWriter.WriteLineAsync();
            }
        }
    }

}


