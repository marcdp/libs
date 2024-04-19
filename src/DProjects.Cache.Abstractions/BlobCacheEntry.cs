using System;
using System.Collections.Specialized;
using System.IO;

namespace DProjects.Cache {

    public class BlobCacheEntry(string key, Stream stream, NameValueCollection headers) : IDisposable {

        //ctor
        public void Dispose() {
            Stream.Dispose();
        }

        //props
        public string Key { get; } = key;
        public Stream Stream { get; } = stream;
        public NameValueCollection Headers { get; } = headers;
    }

}
