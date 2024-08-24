using DProjects.Streams;
using DProjects.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DProjects.Fs;
using Microsoft.Extensions.Logging;

namespace DProjects.Fs.Http {


    public class FilesystemHttp : FilesystemAsync {

        //Modes
        public enum AuthSchemes {
            None,
            Basic,
            Hmac,
            ApiKey
        }

        //constants
        public const string MIMETYPE_FS_ENTRY = "application/vnd.dprojects.fs.entry+json";
        public const string MIMETYPE_FS_ENTRIES = "application/vnd.dprojects.fs.entries+json";
        public const string MIMETYPE_FS_METADATA = "application/vnd.dprojects.fs.metadata+json";
        public const string MIMETYPE_FS_ENTRY_DIRECTORY = "application/vnd.dprojects.fs.entry.directory+json";
        public const string MIMETYPE_FS_COPY = "application/vnd.dprojects.fs.copy+json";
        public const string MIMETYPE_FS_MOVE = "application/vnd.dprojects.fs.move+json";
        public const string MIMETYPE_FS_SYNC = "application/vnd.dprojects.fs.sync+json";
        public const string MIMETYPE_FS_SELECT = "application/vnd.dprojects.fs.select+json";
        public const string MIMETYPE_FS_APPEND = "application/vnd.dprojects.fs.append+json";
        public const string MIMETYPE_FS_SUPPORTS = "application/vnd.dprojects.fs.supports+json";
        public const string HEADER_FS_PATH_PREFIX = "X-DPROJECTS-FS-PATH-PREFIX";
        public const string HEADER_FS_IF_ENTRY_FILE = "X-DPROJECTS-IF-ENTRY-TYPE";

        //Request
        public class PatchRequest {
            public IDictionary<string, string>? Metadata { get; set; }
            public DateTime? Modified { get; set; }
        }
        public class CopyRequest {
            public string Source { get; set; } = "";
            public bool IgnoreErrors { get; set; }
            public bool Overwrite { get; set; }
            public bool Recursive { get; set; }
            public int Tries { get; set; } = 1;
        }
        public class MoveRequest {
            public string Source { get; set; } = "";
            public bool IgnoreErrors { get; set; }
        }
        public class SyncRequest {
            public string Source { get; set; } = "";
            public string[] DestinationExcludes { get; set; } = [];
            public string[] SourceExcludes { get; set; } = [];
            public bool IgnoreErrors { get; set; }
            public int Tries { get; set; } = 1;
            public SyncModes Mode { get; set; }
        }

        //variables
        private readonly Uri mUrl;
        private readonly HttpClient mHttpClient;
        private readonly int mMaxFileUploadSize;
        private readonly AuthSchemes mAuthScheme;

        //constructor
        public FilesystemHttp(Uri url, int maxFileUploadSize, AuthSchemes authScheme, bool isReadOnly) : base(isReadOnly) {
            mUrl = url;
            mMaxFileUploadSize = maxFileUploadSize;
            mAuthScheme = authScheme;
            if (mUrl.AbsolutePath.Length > 1 && mUrl.AbsolutePath.EndsWith("/")) throw new Exception("Url should not end with /");
            var httpClientHandler = new HttpClientHandler();
            mHttpClient = new HttpClient(httpClientHandler);
            mHttpClient.BaseAddress = mUrl;
            mHttpClient.Timeout = TimeSpan.FromHours(4);
        }
        public override void Dispose() {
            mHttpClient.Dispose();
            base.Dispose();
        }


        //properties
        public override string Url {
            get {
                return mUrl.ToString();
            }
        }


        //methods LEVEL 0
        public override async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Get, path, "");
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get entry: " + httpResponse.StatusCode);
                }
                var fsPathPrefix = ExtractPathPrefix(httpResponse);
                var entry = EntryFactory.FromJson(json, fsPathPrefix, mUrl.AbsolutePath);
                return entry;
            }
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Get, path, "mode=" + mode.ToString().ToLower() + (pattern != null ? "&pattern=" + pattern : ""));
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRIES));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    throw new Exception("Unable to get entries: " + httpResponse.StatusCode);
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception("Unable to get entries: " + httpResponse.StatusCode + " (" + json + ")");
                }
                var fsPathPrefix = ExtractPathPrefix(httpResponse);
                using (var responseStream = await httpResponse.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(responseStream)) {
                    do {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        var entry = EntryFactory.FromJson(line, fsPathPrefix, mUrl.AbsolutePath);
                        if (entry != null) yield return entry;
                    } while (true);
                }
            }
        }
        public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            var result = false;
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Head, path, "");
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            SignRequest(httpRequest);
            using (var response = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NoContent) {
                    result = true;
                }
            }
            return result;
        }
        public override async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings? settings, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Get, path, "");
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                if (settings.Length == -1 || settings.Length == long.MaxValue) {
                    httpRequest.Headers.Range = new RangeHeaderValue(settings.Offset, null);
                } else {
                    httpRequest.Headers.Range = new RangeHeaderValue(settings.Offset, settings.Offset + settings.Length - 1);
                }
            }
            SignRequest(httpRequest);
            var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                httpResponse.Dispose();
                throw new Exception("Not found");
            } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.PartialContent) {
                httpResponse.Dispose();
                throw new Exception("Unable to loadReadStream: " + httpResponse.StatusCode);
            }
            var stream = await httpResponse.Content.ReadAsStreamAsync();
            var result = new DProjects.Streams.DisposableStream(stream, ()=>{
                httpResponse.Dispose();
            }, true);
            return result;
        } 


        //method1 LEVEL 1
        public override async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(path, cancellationToken);
            return (entry != null && entry.EntryType == EntryType.File);
        }
        public override async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(path, cancellationToken);
            return (entry != null && entry.EntryType == EntryType.Directory);
        }

        //methods LEVEL 2
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (mMaxFileUploadSize > 0) {
                long offset = 0;
                do {
                    using (var streamPart = new LimitedInputStream(stream, mMaxFileUploadSize, true)) {
                        var httpRequest = CreateHttpRequest(HttpMethod.Put, path, "");
                        httpRequest.Content = new StreamContent(streamPart);
                        if (settings.Append || offset > 0) httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MIMETYPE_FS_APPEND);
                        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
                        SignRequest(httpRequest);
                        using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                            var json = await httpResponse.Content.ReadAsStringAsync();
                            if (httpResponse.StatusCode == HttpStatusCode.MethodNotAllowed) {
                                throw new Exception("Unable to modify filesystem: filesystem is readonly");
                            } else if (httpResponse.StatusCode != HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                                throw new Exception("Unable to save file: " + httpResponse.StatusCode);
                            }
                            var fsPathPrefix = ExtractPathPrefix(httpResponse);
                            var entry = EntryFactory.FromJson(json, fsPathPrefix, mUrl.AbsolutePath);
                            if (entry == null) throw new NullReferenceException();
                            offset += streamPart.BytesRead;
                            if (streamPart.BytesRead < mMaxFileUploadSize) {
                                return entry;
                            }
                        }
                    }
                } while (true);
            } else {
                var httpRequest = CreateHttpRequest(HttpMethod.Put, path, "");
                httpRequest.Content = new StreamContent(stream);
                if (settings.Append) httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MIMETYPE_FS_APPEND);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
                SignRequest(httpRequest);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                    var json = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                        throw new Exception("Unable to modify filesystem: filesystem is readonly");
                    } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                        throw new Exception("Unable to save file: " + httpResponse.StatusCode);
                    }
                    var fsPathPrefix = ExtractPathPrefix(httpResponse);
                    var entry = EntryFactory.FromJson(json, fsPathPrefix, mUrl.AbsolutePath);
                    if (entry == null) throw new NullReferenceException();
                    return entry;
                }
            }
        }
        public override async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, path, "");
            httpRequest.Content = new StringContent("", System.Text.Encoding.UTF8, MIMETYPE_FS_ENTRY_DIRECTORY);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to create directory: " + httpResponse.StatusCode);
                }
                var fsPathPrefix = ExtractPathPrefix(httpResponse);
                var entry = EntryFactory.FromJson(json, fsPathPrefix, mUrl.AbsolutePath);
                if (entry == null) throw new NullReferenceException();
                return entry;
            }
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Delete, path, "");
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to delete file: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(new HttpMethod("PATCH"), path, "");
            var data = new Dictionary<string, object?>();
            if (aDate == default(DateTime)) {
                data.Add("modified", null);
            } else {
                data.Add("modified", aDate);
            }
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, MIMETYPE_FS_ENTRY);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    throw new Exception("Not found");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to touch: " + httpResponse.StatusCode);
                }
            }
        }



        //method LEVEL 3
        public override async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Delete, path, "");
            httpRequest.Headers.Add(HEADER_FS_IF_ENTRY_FILE, "file");
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly: " + httpResponse.StatusCode);
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to delete file: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Delete, path, "");
            httpRequest.Headers.Add(HEADER_FS_IF_ENTRY_FILE, "dir");
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return;
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to delete file: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            PathUtils.Validate(source);
            PathUtils.Validate(destination);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, destination, "");
            var copyRequest = new CopyRequest();
            copyRequest.Source = PathUtils.Combine(mUrl.AbsolutePath, source);
            copyRequest.IgnoreErrors = settings.IgnoreErrors;
            copyRequest.Overwrite = settings.Overwrite;
            copyRequest.Recursive = settings.Recursive;
            copyRequest.Tries = settings.Tries;
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(copyRequest), System.Text.Encoding.UTF8, MIMETYPE_FS_COPY);
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to copy: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            PathUtils.Validate(source);
            PathUtils.Validate(destination);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, destination, "");
            var moveRequest = new MoveRequest();
            moveRequest.Source = PathUtils.Combine(mUrl.AbsolutePath, source);
            moveRequest.IgnoreErrors = settings.IgnoreErrors;
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(moveRequest), System.Text.Encoding.UTF8, MIMETYPE_FS_MOVE);
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to move: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            PathUtils.Validate(source);
            PathUtils.Validate(destination);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, destination, "");
            var syncRequest = new SyncRequest();
            syncRequest.Source = PathUtils.Combine(mUrl.AbsolutePath, source);
            syncRequest.DestinationExcludes = syncSettings.DestinationExcludes;
            syncRequest.SourceExcludes = syncSettings.SourceExcludes;
            syncRequest.IgnoreErrors = syncSettings.IgnoreErrors;
            syncRequest.Mode = syncSettings.Mode;
            syncRequest.Tries = syncSettings.Tries;
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(syncRequest), System.Text.Encoding.UTF8, MIMETYPE_FS_SYNC);
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.Created) {
                    throw new Exception("Unable to sync: " + httpResponse.StatusCode);
                }
            }
        }


        //method LEVEL 4
        public override Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        public override async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(HttpMethod.Get, path, "");
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_METADATA));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    throw new Exception("Unable to get metadata: " + httpResponse.StatusCode);
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get metadata: " + httpResponse.StatusCode);
                }
                return System.Text.Json.JsonSerializer.Deserialize<IDictionary<string, string>>(json)!;
            }

        }
        public override async Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            var httpRequest = CreateHttpRequest(new HttpMethod("PATCH"), path, "");
            var patchRequest = new PatchRequest();
            patchRequest.Metadata = metadata;
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(patchRequest), System.Text.Encoding.UTF8, MIMETYPE_FS_ENTRY);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_ENTRY));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed) {
                    throw new Exception("Unable to modify filesystem: filesystem is readonly");
                } else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    throw new Exception("Not found");
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to set metadata: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            var httpRequest = CreateHttpRequest(HttpMethod.Get, path, "feature=" + feature.ToString());
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MIMETYPE_FS_SUPPORTS));
            SignRequest(httpRequest);
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                var json = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    throw new Exception("Unable to get entry: " + httpResponse.StatusCode);
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get entry: " + httpResponse.StatusCode);
                }
                return System.Text.Json.JsonSerializer.Deserialize<bool>(json);
            }
        }


        //utils
        private HttpRequestMessage CreateHttpRequest(HttpMethod method, string path, string querystring) {
            var aux = PathUtils.Combine(mUrl.AbsolutePath, PathUtils.GetPathURLEncoded(path)) + (querystring.Length > 0 ? "?" + querystring : "");
            Uri requestUri = new Uri(aux, UriKind.Relative);
            var request = new HttpRequestMessage(method, requestUri);
            return request;
        }
        private string ExtractPathPrefix(HttpResponseMessage httpResponse) {
            if (httpResponse.Headers.TryGetValues(HEADER_FS_PATH_PREFIX, out var values)) {
                foreach (var value in values) return value;
            }
            return "";
        }
        private void SignRequest(HttpRequestMessage httpRequest) {
            if (mAuthScheme == AuthSchemes.None) {
            } else if (mAuthScheme == AuthSchemes.Basic) {
                if (!string.IsNullOrEmpty(mUrl.UserInfo)) {
                    var userInfo = mUrl.UserInfo + (mUrl.UserInfo.IndexOf(":") == -1 ? ":" : "");
                    var login = userInfo.Split(':')[0];
                    var password = UrlUtils.UrlDecode(userInfo.Split(':')[1]);
                    var credentials = new NetworkCredential(login, password);
                    var value = DProjects.Utils.AuthHttpBasicUtils.CreateHeader(credentials);
                    httpRequest.Headers.Add(HttpUtils.HEADER_AUTHORIZATION, value);
                }
            } else if (mAuthScheme == AuthSchemes.Hmac) {
                if (!string.IsNullOrEmpty(mUrl.UserInfo)) {
                    var userInfo = mUrl.UserInfo + (mUrl.UserInfo.IndexOf(":") == -1 ? ":" : "");
                    var login = userInfo.Split(':')[0];
                    var password = UrlUtils.UrlDecode(userInfo.Split(':')[1]);
                    var key = Convert.FromBase64String(password);
                    var credentials = new NetworkCredential(login, password);
                    var path = httpRequest.RequestUri.OriginalString;
                    var query = "";
                    if (path.IndexOf("?") != -1) {
                        query = path.Substring(path.IndexOf("?"));
                        path = path.Substring(0, path.IndexOf("?"));
                    }
                    var queryDecoded = UrlUtils.UrlDecode(query);
                    var contentType = "";
                    if (httpRequest.Content != null && httpRequest.Content.Headers.ContentType != null) {
                        contentType = httpRequest.Content.Headers.ContentType.ToString();
                    }
                    DateTime dateToUse = DateTime.Now;
                    var value = DProjects.Utils.AuthHttpHmacUtils.CreateHeader(
                        login,
                        key,
                        httpRequest.Method.Method,
                        path,
                        queryDecoded,
                        contentType,
                        dateToUse,
                        default(DateTime)
                    );
                    httpRequest.Headers.Add(HttpUtils.HEADER_AUTHORIZATION, value);
                    httpRequest.Headers.Add(HttpUtils.HEADER_DATE, dateToUse.ToUniversalTime().ToString("r"));
                }
            } else if (mAuthScheme == AuthSchemes.ApiKey) {
                if (!string.IsNullOrEmpty(mUrl.UserInfo)) {
                    var value = "ApiKey " + mUrl.UserInfo;
                    httpRequest.Headers.Add(HttpUtils.HEADER_AUTHORIZATION, value);
                }
            }
        }

    }

}


