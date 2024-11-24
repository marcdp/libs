
using System;
using System.IO;

using DProjects.Utils;

namespace DProjects.Cache {

    public class BlobCacheEntry : IDisposable {


        //ctor
        public BlobCacheEntry(string key, Stream stream, HeadersUtils.Headers? headers = null) {
            Key = key;
            Headers = headers ?? new HeadersUtils.Headers();
            Stream = stream;
        }
        public void Dispose() {
            Stream.Dispose(); 
        }


        //props
        public string Key { get; }
        public HeadersUtils.Headers Headers { get; }
        public string? ContentType {
            get => Headers.Get<string?>(HttpUtils.HEADER_CONTENT_TYPE, null);
            set => Headers.Set(HttpUtils.HEADER_CONTENT_TYPE, value);
        }
        public long? ContentLength {
            get => Headers.Get<long?>(HttpUtils.HEADER_CONTENT_LENGTH, null);
            set => Headers.Set(HttpUtils.HEADER_CONTENT_LENGTH, value);
        }
        public string? Etag {
            get => Headers.Get<string?>(HttpUtils.HEADER_ETAG, null);
            set => Headers.Set(HttpUtils.HEADER_ETAG, value);
        }
        public DateTime? Expires {
            get => Headers.Get<DateTime?>(HttpUtils.HEADER_EXPIRES, null);
            set => Headers.Set(HttpUtils.HEADER_EXPIRES, value);
        }
        public DateTime? LastModified {
            get => Headers.Get<DateTime?>(HttpUtils.HEADER_LAST_MODIFIED, null);
            set => Headers.Set(HttpUtils.HEADER_LAST_MODIFIED, value);
        }
        public DateTime? Date {
            get => Headers.Get<DateTime?>(HttpUtils.HEADER_DATE, null);
            set => Headers.Set(HttpUtils.HEADER_DATE, value);
        }
        public Stream Stream{ get; }

    }

}
