using DProjects.Streams;
using DProjects.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Xml;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DProjects.Fs.Aws {


    public class FilesystemS3 : FilesystemAsync {


        //vars
        private readonly string mBucket;
        private readonly string mRegion;
        private readonly string mAccessKeyId;
        private readonly string mSecretAccesKey;
        private readonly string mService;
        private readonly string mBasePath;
        private readonly bool mAutoGzip;
        private readonly bool mAutoCache;
        private readonly long mUploadPartSize;
        private readonly DateTime mStartDate;
        private readonly HttpClient mHttpClient;


        //constructor
        public FilesystemS3(string bucket, string region, string accessKeyId, string secretAccesKey, string basePath, bool autoGzip, bool autoCache, bool isReadOnly, HttpClientHandler? httpClientHandler = null) : base(isReadOnly) {
            mBucket = bucket;
            mRegion= region;
            mAccessKeyId = accessKeyId;
            mSecretAccesKey = secretAccesKey;
            mService = "s3";
            mBasePath = (basePath.EndsWith("/") ? basePath.Substring(0, basePath.Length - 1) : basePath);
            mAutoGzip = autoGzip;
            mAutoCache = autoCache;
            mUploadPartSize = 50 * 1024 * 1024;
            mStartDate = DateTime.Now;
            //http client
            mHttpClient = new HttpClient(httpClientHandler ?? new HttpClientHandler());
            mHttpClient.BaseAddress = new Uri("https://" + bucket + ".s3" + (!string.IsNullOrEmpty(region) ? "-" + region : "") + ".amazonaws.com");
            mHttpClient.Timeout = TimeSpan.FromHours(1);
        }
        public override void Dispose() {  
            mHttpClient.Dispose();
            base.Dispose();
        }


        //properties
        public override string Url {
            get {
                var query = new List<string>();
                if (mAutoGzip) query.Add("autogzip=true");
                return "s3://" + UrlUtils.UrlEncode(mAccessKeyId) + ":" + UrlUtils.UrlEncode(mSecretAccesKey) + "@" + mBucket + ".s3" + (!string.IsNullOrEmpty(mRegion) ? "-" + mRegion : "") + ".amazonaws.com/" + (query.Count > 0 ? "?" + string.Join("&", query.ToArray()) : "");
            }
        }


        //methods LEVEL 0
        public override async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (path.Equals("/")) {
                DateTime d = mStartDate;
                return new Entry("/", EntryType.Directory, mStartDate, mStartDate, 0, "", 0);
            } else {
                var querystring = "?list-type=2&prefix=" + Utils.UriEncode(mBasePath + path, false).Substring(1) + "&delimiter=/";
                var httpRequest = CreateHttpRequest(HttpMethod.Get, "/", querystring);
                SignRequest(httpRequest, "/", querystring);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                    var xml = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                        throw new Exception("Unable to get getentry: " + httpResponse.StatusCode);
                    }
                    var xmlDocument = XmlUtils.LoadXml(xml);
                    var xmlDocumentElement = xmlDocument.DocumentElement;
                    if (xmlDocumentElement != null) {
                        var xlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                        xlNamespaceManager.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                        var xmlIsTruncated = xmlDocumentElement.SelectSingleNode("//s3:IsTruncated", xlNamespaceManager);
                        var hasMoreEntries = (xmlIsTruncated != null) && StringUtils.Equals(xmlIsTruncated.InnerText, "true");
                        var xmlNodeNextMarker = xmlDocumentElement.SelectSingleNode("//s3:NextMarker", xlNamespaceManager);
                        var xmlNodeContents = xmlDocumentElement.SelectNodes("//s3:Contents", xlNamespaceManager);
                        if (xmlNodeContents != null) {
                            foreach (XmlNode? xmlNode in xmlNodeContents) {
                                if (xmlNode != null) {
                                    var entry = CreateEntryFromXmlNode(xmlNode, xlNamespaceManager);
                                    if (entry.Path.Equals(path)) return entry;
                                }
                            }
                        }
                        var xmlCommonPrefixes = xmlDocumentElement.SelectNodes("//s3:CommonPrefixes", xlNamespaceManager);
                        if (xmlCommonPrefixes != null) {
                            foreach (XmlNode? xmlNode in xmlCommonPrefixes) {
                                if (xmlNode != null) {
                                    var entry = CreateEntryFromXmlNode(xmlNode, xlNamespaceManager);
                                    if (entry.Path.Equals(path)) return entry;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            PathUtils.Validate(path);
            var querystring = "";
            if (mode == GetModes.All) {
                querystring = "?list-type=2&prefix=" + Utils.UriEncode(mBasePath + path, false).Substring(1) + (path.Length > 1 ? "/" : "") + "&delimiter=/";
            } else if (mode == GetModes.Files) {
                querystring = "?list-type=2&prefix=" + Utils.UriEncode(mBasePath + path, false).Substring(1) + (path.Length > 1 ? "/" : "") + "&delimiter=/";
            } else if (mode == GetModes.Directories) {
                querystring = "?list-type=2&prefix=" + Utils.UriEncode(mBasePath + path, false).Substring(1) + (path.Length > 1 ? "/" : "") + "&delimiter=/";
            } else if (mode == GetModes.Descendants) {
                querystring = "?list-type=2&prefix=" + Utils.UriEncode(mBasePath + path, false).Substring(1) + (path.Length > 1 ? "/" : "") + "&delimiter=/";
            }
            string? nextContinuationToken = null;
            var all = new List<Entry>();
            do {
                var queryParamContinuationToken = (nextContinuationToken != null ? "&continuation-token=" + Utils.UriEncode(nextContinuationToken, false) : "");
                var httpRequest = CreateHttpRequest(HttpMethod.Get, "/", querystring + queryParamContinuationToken);
                SignRequest(httpRequest, "/", querystring + queryParamContinuationToken);
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                    var xml = await httpResponse.Content.ReadAsStringAsync();
                    var aux = httpResponse.Headers.GetValues("x-amz-bucket-region");
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                        throw new Exception("Unable to get entries: " + httpResponse.StatusCode);
                    }
                    var xmlDocument = XmlUtils.LoadXml(xml);
                    var xmlDocumentElement = xmlDocument.DocumentElement;
                    if (xmlDocumentElement != null) {
                        var xlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                        xlNamespaceManager.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                        var xmlIsTruncated = xmlDocumentElement.SelectSingleNode("//s3:IsTruncated", xlNamespaceManager);
                        var hasMoreEntries = (xmlIsTruncated != null) && StringUtils.Equals(xmlIsTruncated.InnerText, "true");
                        var xmlNextContinuationToken = xmlDocumentElement.SelectSingleNode("//s3:NextContinuationToken", xlNamespaceManager);
                        if (xmlNextContinuationToken != null) {
                            nextContinuationToken = xmlNextContinuationToken.InnerText;
                        } else {
                            nextContinuationToken = null;
                        }
                        var xmlNodeContents = xmlDocumentElement.SelectNodes("//s3:Contents", xlNamespaceManager);
                        if (xmlNodeContents != null) {
                            foreach (XmlNode? xmlNode in xmlNodeContents) {
                                if (xmlNode != null) {
                                    var entry = CreateEntryFromXmlNode(xmlNode, xlNamespaceManager);
                                    if (!entry.Path.Equals(path)) {
                                        all.Add(entry);
                                    }
                                }
                            }
                        }
                        var xmlCommonPrefixes = xmlDocumentElement.SelectNodes("//s3:CommonPrefixes", xlNamespaceManager);
                        if (xmlCommonPrefixes != null) {
                            foreach (XmlNode? xmlNode in xmlCommonPrefixes) {
                                if (xmlNode != null) {
                                    var entry = CreateEntryFromXmlNode(xmlNode, xlNamespaceManager);
                                    if (!entry.Path.Equals(path)) {
                                        all.Add(entry);
                                    }
                                }
                            }
                        }
                    }
                }
            } while (nextContinuationToken != null);
            all.Sort(new EntryComparer());
            foreach (var entry in all) {
                var isValid = false;
                if (entry.IsFile() && (mode == GetModes.All || mode == GetModes.Files || mode == GetModes.Descendants)) isValid = true;
                if (entry.IsDirectory() && (mode == GetModes.All || mode == GetModes.Directories || mode == GetModes.Descendants)) isValid = true;

                if (isValid) {
                    if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                        yield return entry;
                    }
                }
                if (mode == GetModes.Descendants && entry.IsDirectory()) {
                    await foreach (var subentry in GetEntriesAsync(entry.Path, mode, pattern, cancellationToken)) {
                        yield return subentry;
                    }
                }
            }
        }
        public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            return (await GetEntryAsync(path, cancellationToken) != null);
        }
        public override async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings? settings = null, CancellationToken cancellationToken = default) {
            PathUtils.Validate(path);
            var gzipped = (mAutoGzip && MimeTypeUtils.IsCompressible(MimeTypeUtils.GetMimeType(path)));
            var httpRequest = CreateHttpRequest(HttpMethod.Get, mBasePath + path, "");
            if (!gzipped && settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                if (settings.Length == -1 || settings.Length == long.MaxValue) {
                    httpRequest.Headers.Range = new RangeHeaderValue(settings.Offset, null);
                } else {
                    httpRequest.Headers.Range = new RangeHeaderValue(settings.Offset, settings.Offset + settings.Length - 1);
                }
            }
            SignRequest(httpRequest, mBasePath + path, "");
            var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK && httpResponse.StatusCode != System.Net.HttpStatusCode.PartialContent) {
                httpResponse.Dispose();
                throw new Exception("Unable to load read stream: " + httpResponse.StatusCode);
            }
            Stream result = new DProjects.Streams.DisposableStream(await httpResponse.Content.ReadAsStreamAsync(), () => {
                httpResponse.Dispose();
            });
            if (gzipped && settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                if (settings.Offset != 0) {
                    await StreamUtils.ConsumeAsync(result, settings.Offset);
                }
                if (settings.Length != -1) {
                    result = new LimitedInputStream(result, settings.Length);
                }
            }
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

        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings? settings = null, CancellationToken cancellationToken = default) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            string mimetype = MimeTypeUtils.GetMimeType(path);
            bool gzip = mAutoGzip && MimeTypeUtils.IsCompressible(mimetype);
            var pathParent = PathUtils.GetPathParent(path);
            if (!await ExistsDirectoryAsync(pathParent, cancellationToken)) throw new Exception("Unable to save file: parent path not found: " + path);
            //append
            var append = (settings != null && settings.Append);
            if (append && await ExistsFileAsync(path, cancellationToken)) {
                using (var concatenatedStream = new CatInputStream(new Stream[] { await LoadReadStreamAsync(path, new(), cancellationToken), stream })) {
                    return await SaveFileAsync(path, concatenatedStream);
                }
            }
            //temp file
            var tempFilename = System.IO.Path.GetTempFileName();
            var uploadPartSize = mUploadPartSize;
            try {
                using (var tempStream = new FileStream(tempFilename, FileMode.Truncate, FileAccess.ReadWrite)) {
                    //consume
                    var bytesReaded = await StreamUtils.CopyAsync(new LimitedInputStream(stream, uploadPartSize, true), tempStream);
                    //hash
                    tempStream.Seek(0, SeekOrigin.Begin);
                    byte[]? sha256 = null;
                    using (var sha256Managed = SHA256.Create()) {
                        sha256 = sha256Managed.ComputeHash(tempStream);
                    }
                    tempStream.Seek(0, SeekOrigin.Begin);
                    //decide single vs multipart upload
                    if (bytesReaded < uploadPartSize) {
                        //single upload                        
                        var httpRequest = CreateHttpRequest(HttpMethod.Put, mBasePath + path, "");
                        httpRequest.Content = new StreamContent(tempStream);
                        if (mAutoCache) httpRequest.Headers.CacheControl = CreateAutoCacheHeaderValue(mimetype, cancellationToken);
                        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimetype);
                        SignRequest(httpRequest, mBasePath + path, "", ConvertUtils.ToHexString(sha256).ToLower());
                        using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to save file: " + httpResponse.StatusCode);
                            var etag = httpResponse.Headers.ETag?.Tag.Replace("\"", "") ?? "";
                            var aDateTimeOffset = httpResponse.Headers.Date ?? new DateTimeOffset();
                            return new Entry(path, EntryType.File, aDateTimeOffset.LocalDateTime, aDateTimeOffset.LocalDateTime, bytesReaded, etag, 0);
                        }
                    } else {
                        //multipart upload
                        var bytesUploaded = (long)0;
                        var uploadId = "";
                        var partsEtags = new List<string>();
                        var httpRequest = CreateHttpRequest(HttpMethod.Post, mBasePath + path, "?uploads");
                        if (mAutoCache) httpRequest.Headers.CacheControl = CreateAutoCacheHeaderValue(mimetype, cancellationToken);

                        httpRequest.Content = new ByteArrayContent(new byte[] { });
                        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimetype);

                        SignRequest(httpRequest, mBasePath + path, "?uploads", ConvertUtils.ToHexString(sha256).ToLower());
                        using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to save file: unable to init multipart upload: " + httpResponse.StatusCode);
                            var xml = await httpResponse.Content.ReadAsStringAsync();
                            var xmlDocument = XmlUtils.LoadXml(xml);
                            var xmlDocumentElement = xmlDocument.DocumentElement;
                            if (xmlDocumentElement != null) {
                                var xlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                                xlNamespaceManager.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                                uploadId = xmlDocumentElement.SelectSingleNode("//s3:InitiateMultipartUploadResult/s3:UploadId", xlNamespaceManager)?.InnerText ?? "";
                            }
                        }
                        try {
                            var partNumber = 1;
                            while (bytesReaded > 0) {
                                httpRequest = CreateHttpRequest(HttpMethod.Put, mBasePath + path, "?partNumber=" + partNumber + "&uploadId=" + uploadId);
                                httpRequest.Content = new StreamContent(new Streams.LeaveOpenInputStream(tempStream));
                                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.APPLICATION_OCTET_STREAM);
                                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(mimetype);
                                SignRequest(httpRequest, mBasePath + path, "?partNumber=" + partNumber + "&uploadId=" + uploadId, ConvertUtils.ToHexString(sha256).ToLower());
                                using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                                        var error = await httpResponse.Content.ReadAsStringAsync();
                                        throw new Exception("Unable to save file: unable to upload multipart part: " + httpResponse.StatusCode + ", " + error);
                                    }
                                    bytesUploaded += bytesReaded;
                                    var etag = httpResponse.Headers.ETag?.Tag;
                                    if (etag != null) {
                                        partsEtags.Add(etag);
                                    }
                                }
                                partNumber += 1;
                                //read next part
                                tempStream.Seek(0, SeekOrigin.Begin);
                                tempStream.SetLength(0);
                                bytesReaded = await StreamUtils.CopyAsync(new LimitedInputStream(stream, uploadPartSize, true), tempStream);
                                //hash
                                tempStream.Seek(0, SeekOrigin.Begin);
                                using (var sha256Managed = SHA256.Create()) {
                                    sha256 = sha256Managed.ComputeHash(new LimitedInputStream(tempStream, bytesReaded, true));
                                }
                                tempStream.Seek(0, SeekOrigin.Begin);
                            }
                            //create multipart complete xml
                            var xmlSB = new StringBuilder();
                            using (var xmlWriter = XmlWriter.Create(xmlSB)) {
                                xmlWriter.WriteStartDocument();
                                xmlWriter.WriteStartElement("CompleteMultipartUpload");
                                for (var i = 0; i < partsEtags.Count; i++) {
                                    xmlWriter.WriteStartElement("Part");
                                    xmlWriter.WriteStartElement("PartNumber");
                                    xmlWriter.WriteString((i + 1).ToString());
                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteStartElement("ETag");
                                    xmlWriter.WriteString(partsEtags[i]);
                                    xmlWriter.WriteEndElement();
                                    xmlWriter.WriteEndElement();
                                }
                                xmlWriter.WriteEndElement();
                                xmlWriter.WriteEndDocument();
                            }
                            var xmlRequest = xmlSB.ToString();
                            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlRequest.ToString());
                            //complete multipart upload
                            httpRequest = CreateHttpRequest(HttpMethod.Post, mBasePath + path, "?uploadId=" + uploadId);
                            using (var sha256Managed = SHA256.Create()) {
                                sha256 = sha256Managed.ComputeHash(xmlBytes);
                            }
                            httpRequest.Content = new ByteArrayContent(xmlBytes);
                            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.TEXT_XML);
                            SignRequest(httpRequest, mBasePath + path, "?uploadId=" + uploadId, ConvertUtils.ToHexString(sha256).ToLower());
                            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead)) {
                                var xml = await httpResponse.Content.ReadAsStringAsync();
                                var xmlDocument = XmlUtils.LoadXml(xml);
                                var xmlDocumentElement = xmlDocument.DocumentElement;
                                var xlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                                xlNamespaceManager.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to save file: " + httpResponse.StatusCode + ": " + xml);
                                var etag = (xmlDocumentElement != null ? xmlDocumentElement.InnerText : "");
                                var aDateTimeOffset = httpResponse.Headers.Date ?? new DateTimeOffset();
                                return new Entry(path, EntryType.File, aDateTimeOffset.LocalDateTime, aDateTimeOffset.LocalDateTime, bytesUploaded, etag, 0);
                            }
                        } catch (Exception e) {
                            //cancel multipart upload upload
                            var message = e.Message;
                            httpRequest = CreateHttpRequest(HttpMethod.Delete, mBasePath + path, "?uploadId=" + uploadId);
                            SignRequest(httpRequest, mBasePath + path, "?uploadId=" + uploadId, ConvertUtils.ToHexString(sha256).ToLower());
                            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                                if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) throw new Exception("Unable to save file: unable to delete multipart upload: " + httpResponse.StatusCode);
                            }
                            throw;
                        }
                    }
                }
            } finally {
                FileUtils.DeleteFile(tempFilename);
            }
        }
        public override async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var pathParent = PathUtils.GetPathParent(path);
            if (!ExistsDirectory(pathParent)) CreateDirectory(pathParent);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, mBasePath + path + "/", "");
            httpRequest.Content = new StringContent("", System.Text.Encoding.ASCII, "text/plain");
            SignRequest(httpRequest, mBasePath + path + "/", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get create directory: " + httpResponse.StatusCode);
                }
                var aDateTimeOffset = httpResponse.Headers.Date ?? new DateTimeOffset();
                return new Entry(path, EntryType.Directory, aDateTimeOffset.LocalDateTime, aDateTimeOffset.LocalDateTime, 0, "", 0);
            }
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var entry = GetEntry(path);
            if (entry != null) {
                if (entry.IsDirectory()) {
                    await DeleteDirectoryAsync(path, cancellationToken);
                } else {
                    await DeleteFileAsync(path, cancellationToken);
                }
            }
        }


        //method LEVEL 3
        public override async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            PathUtils.Validate(source);
            PathUtils.Validate(destination);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var entrySource = await GetEntryAsync(source, cancellationToken);
            if (entrySource == null) {
                throw new Exception("Unable to copy: not found " + source);
            } else if (entrySource.IsDirectory()) {
                if (!await ExistsDirectoryAsync(destination, cancellationToken)) {
                    logger.LogInformation(entrySource.Path + " -> " + destination);
                    await CreateDirectoryAsync(destination, cancellationToken);
                    var metadata = await GetMetadataAsync(source, cancellationToken);
                    if (metadata.Count > 0) {
                        await SetMetadataRawAsync(destination, metadata);
                    }
                }
                if (settings.Recursive) {
                    await foreach (Entry entryChildsource in GetEntriesAsync(source)) {
                        string childPath = PathUtils.Combine(destination, entryChildsource.Name);
                        try {
                            await CopyAsync(entryChildsource.Path, childPath, settings, logger, cancellationToken);
                        } catch (Exception ex) {
                            if (settings.IgnoreErrors) {
                                logger.LogError(ex.Message);
                            } else {
                                throw;
                            }
                        }
                    }
                } else {
                    logger.LogInformation("Omitting directory {0}", destination);
                }
            } else {
                var entryDestination = await GetEntryAsync(destination, cancellationToken);
                if (entryDestination == null) {
                    await CopyFileAsync(entrySource, destination, settings, logger, cancellationToken);
                } else if (!entryDestination.IsDirectory()) {
                    if (settings.Overwrite) {
                        await CopyFileAsync(entrySource, destination, settings, logger, cancellationToken);
                    }
                } else if (entryDestination.IsDirectory()) {
                    string destinationInDirectory = PathUtils.Combine(destination, PathUtils.GetPathName(source));
                    var entryDestinationInDirectory = GetEntry(destinationInDirectory);
                    if (entryDestinationInDirectory == null) {
                        await CopyFileAsync(entrySource, destinationInDirectory, settings, logger, cancellationToken);
                    } else if (!entryDestination.IsDirectory()) {
                        throw new Exception("Unable to copy: destination exists and is a directory");
                    } else {
                        if (settings.Overwrite) {
                            await CopyFileAsync(entrySource, destinationInDirectory, settings, logger, cancellationToken);
                        }
                    }
                }
            }
        }
        private async Task CopyFileAsync(Entry aSource, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            for (int trie = 0; trie <= settings.Tries - 1; trie++) {
                logger.LogInformation(aSource.Path + " -> " + destination);
                try {
                    var httpRequest = CreateHttpRequest(HttpMethod.Put, mBasePath + destination, "");
                    httpRequest.Headers.Add("x-amz-copy-source", "/" + mBucket + Utils.UriEncode(aSource.Path, false));
                    SignRequest(httpRequest, mBasePath + destination, "");
                    using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                        if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                            throw new Exception("Unable to copy file: " + httpResponse.StatusCode);
                        }
                    }
                    return;
                } catch (Exception ex) {
                    logger.LogError(ex, "Unable to copy from {0}, to {1} ({2}/{3}): {4}", aSource.Path, destination, trie + 1, settings.Tries, ex.Message);
                    if (trie == settings.Tries - 1) {
                        if (!settings.IgnoreErrors) throw;
                    } else {
                        await Task.Delay(250);
                    }
                }
            }
        }
        public override async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var httpRequest = CreateHttpRequest(HttpMethod.Delete, mBasePath + path, "");
            httpRequest.Content = new StringContent("", System.Text.Encoding.ASCII, "text/plain");
            SignRequest(httpRequest, mBasePath + path, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) {
                    throw new Exception("Unable to get delete file: " + httpResponse.StatusCode);
                }
            }
        }
        public override async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var entryRoot = GetEntry(path);
            if (entryRoot == null) throw new Exception("Unable to delete directory \'" + path + "\': not found");
            var entries = new List<Entry>();
            await foreach (var entry in GetEntriesAsync(path, GetModes.Descendants, null, cancellationToken)) {
                entries.Add(entry);
            }
            entries.Add(entryRoot);
            entries.Sort(new EntryComparer());
            entries.Reverse();
            foreach (var entry in entries) {
                if (entry.IsDirectory()) {
                    var httpRequest = CreateHttpRequest(HttpMethod.Delete, mBasePath + entry.Path + "/", "");
                    httpRequest.Content = new StringContent("", System.Text.Encoding.ASCII, "text/plain");
                    SignRequest(httpRequest, mBasePath + entry.Path + "/", "");
                    using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                        if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) {
                            throw new Exception("Unable to delete directory: " + httpResponse.StatusCode);
                        }
                    }
                } else {
                    DeleteFile(entry.Path);
                }
            }
        }


        //method LEVEL 4
        public override async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            return await GetMetadataRawAsync(path, cancellationToken);
        }
        private async Task<IDictionary<string, string>> GetMetadataRawAsync(string path, CancellationToken cancellationToken) {
            var httpRequest = CreateHttpRequest(HttpMethod.Head, mBasePath + path, "");
            SignRequest(httpRequest, mBasePath + path, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                var res = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return await GetMetadataRawAsync(path + "/", cancellationToken);
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to get metadata: " + httpResponse.StatusCode);
                }
                var metadata = new Dictionary<string, string>();
                foreach (var header in httpResponse.Headers) {
                    if (header.Key.StartsWith("x-amz-meta-")) {
                        var key = header.Key.Substring("x-amz-meta-".Length);
                        var value = "";
                        foreach (var val in header.Value) value += UrlUtils.UrlDecode(val);
                        metadata[key] = value;
                    }
                }
                return metadata;
            }
        }
        public override async Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            await SetMetadataRawAsync(path, metadata);
        }
        private async Task SetMetadataRawAsync(string path, IDictionary<string, string> metadata) {
            var httpRequest = CreateHttpRequest(HttpMethod.Put, mBasePath + path, "");
            httpRequest.Headers.Add("x-amz-copy-source", "/" + mBucket + Utils.UriEncode(path, false));
            httpRequest.Headers.Add("x-amz-metadata-directive", "REPLACE");
            var keysAdded = new List<string>();
            foreach (var key in metadata.Keys) {
                var keyToUse = key.ToLower().Trim();
                if (!keysAdded.Contains(keyToUse)) {
                    httpRequest.Headers.Add("x-amz-meta-" + keyToUse, UrlUtils.UrlEncode(metadata[key]));
                    keysAdded.Add(keyToUse);
                }
            }
            SignRequest(httpRequest, mBasePath + path, "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                var aux = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound && !path.EndsWith("/")) {
                    await SetMetadataRawAsync(path + "/", metadata);
                } else if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to set metadata: " + httpResponse.StatusCode);
                }
            }
        }
        public override Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            if (feature == Features.Metadata) return Task.FromResult(true);
            return Task.FromResult(false);
        }


        //utils
        private CacheControlHeaderValue CreateAutoCacheHeaderValue(string mimetype, CancellationToken cancellationToken) {
            var seconds = 60 * 60;
            if (MimeTypeUtils.IsImage(mimetype)) {
                seconds = 7 * 24 * 60 * 60; // 7 days
            } else if (MimeTypeUtils.IsFont(mimetype)) {
                seconds = 7 * 24 * 60 * 60; // 7 days
            } else if (MimeTypeUtils.IsVideo(mimetype) || MimeTypeUtils.IsAudio(mimetype)) {
                seconds = 365 * 24 * 60 * 60; // 365 days
            } else if (mimetype.Equals(MimeTypeUtils.TEXT_CSS) || mimetype.Equals(MimeTypeUtils.APPLICATION_JAVASCRIPT) || mimetype.Equals(MimeTypeUtils.APPLICATION_JSON)) {
                seconds = 7 * 24 * 60 * 60; // 7 days
            } else if (mimetype.Equals(MimeTypeUtils.TEXT_HTML)) {
                seconds = 60 * 60; // 1 hour
            } else if (MimeTypeUtils.IsText(mimetype)) {
                seconds = 60 * 60; // 1 hour
            }
            var cacheControlHeader = new CacheControlHeaderValue();
            cacheControlHeader.Public = true;
            cacheControlHeader.MaxAge = TimeSpan.FromSeconds(seconds);
            return cacheControlHeader;
        }
        public async Task CreateBucketAsync(CancellationToken cancellationToken) {
            var xmlSB = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(xmlSB)) {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("CreateBucketConfiguration", "http://s3.amazonaws.com/doc/2006-03-01/");
                xmlWriter.WriteStartElement("LocationConstraint");
                xmlWriter.WriteValue(mRegion);
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
            var xmlRequest = xmlSB.ToString();
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlRequest.ToString());
            var httpRequest = CreateHttpRequest(HttpMethod.Put, "/", "");
            var sha256 = new byte[] { };
            using (var sha256Managed = SHA256.Create()) {
                sha256 = sha256Managed.ComputeHash(xmlBytes);
            }
            httpRequest.Content = new ByteArrayContent(xmlBytes);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.TEXT_XML);
            SignRequest(httpRequest, "/", "", ConvertUtils.ToHexString(sha256).ToLower());
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken)) {
                var xml = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
                    throw new Exception("Unable to create bucket: " + httpResponse.StatusCode);
                }
            }
        }
        public async Task SetBucketTagsAsync(IDictionary<string, string> tags, CancellationToken cancellationToken) {
            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb)) {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Tagging", "http://s3.amazonaws.com/doc/2006-03-01/");
                xmlWriter.WriteStartElement("TagSet");
                foreach (var key in tags.Keys) {
                    xmlWriter.WriteStartElement("Tag");
                    xmlWriter.WriteStartElement("Key");
                    xmlWriter.WriteString(key);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("Value");
                    xmlWriter.WriteString(tags[key]);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
            var xml = sb.ToString();
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);
            var httpRequest = CreateHttpRequest(HttpMethod.Put, "/", "?tagging");
            var sha256 = new byte[] { };
            using (var sha256Managed = SHA256.Create()) {
                sha256 = sha256Managed.ComputeHash(xmlBytes);
            }
            httpRequest.Content = new ByteArrayContent(xmlBytes);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeUtils.TEXT_XML);
            var md5 = new byte[] { };
            using (var md5Managed = MD5.Create()) {
                md5 = md5Managed.ComputeHash(xmlBytes);
            }
            httpRequest.Content.Headers.ContentMD5 = md5;
            SignRequest(httpRequest, "/", "?tagging", ConvertUtils.ToHexString(sha256).ToLower());
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken)) {
                xml = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) {
                    throw new Exception("Unable to set bucket tag: " + httpResponse.StatusCode);
                }
            }
        }
        public async Task RemoveBucketAsync(bool recursive, CancellationToken cancellationToken) {
            if (recursive) {
                await foreach (var entry in GetEntriesAsync("/", GetModes.All, null, cancellationToken)) {
                    Delete(entry.Path);
                }
            }
            var httpRequest = CreateHttpRequest(HttpMethod.Delete, "/", "");
            SignRequest(httpRequest, "/", "");
            using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken)) {
                var xml = await httpResponse.Content.ReadAsStringAsync();
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.NoContent) {
                    throw new Exception("Unable to remove bucket: " + httpResponse.StatusCode);
                }
            }
        }
        private HttpRequestMessage CreateHttpRequest(HttpMethod method, string pathAbsolute, string querystring) {
            var requestUri = new Uri(Utils.UriEncode(pathAbsolute, false) + querystring, UriKind.Relative);
            return new HttpRequestMessage(method, requestUri);
        }
        private void SignRequest(HttpRequestMessage httpRequest, string pathAbsolute, string query, string? contentHasSha256 = null) {
            Utils.SignRequestV4(httpRequest, mRegion, mService, mHttpClient.BaseAddress?.Host ?? "", pathAbsolute, query, mAccessKeyId, mSecretAccesKey, contentHasSha256);
        }
        private Entry CreateEntryFromXmlNode(XmlNode xmlNode, XmlNamespaceManager xmlNamespaceManager) {
            if (xmlNode.Name.Equals("Contents")) {
                string path = "/" + xmlNode.SelectSingleNode("s3:Key", xmlNamespaceManager)?.InnerText;
                var entryType = (path.EndsWith("/") ? EntryType.Directory : EntryType.File);
                DateTime created = default(DateTime);
                DateTime modified = DateTime.Parse(xmlNode.SelectSingleNode("s3:LastModified", xmlNamespaceManager)?.InnerText ?? "", null, System.Globalization.DateTimeStyles.AssumeUniversal);
                long length = System.Convert.ToInt64(xmlNode.SelectSingleNode("s3:Size", xmlNamespaceManager)?.InnerText);
                string etag = xmlNode.SelectSingleNode("s3:ETag", xmlNamespaceManager)?.InnerText.Replace("\"", "") ?? "";
                path = PathUtils.Uncombine(mBasePath, path);
                if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
                int flags = 0;
                return new Entry(path, entryType, created, modified, length, etag, flags);
            } else if (xmlNode.Name.Equals("CommonPrefixes")) {
                string path = "/" + xmlNode.SelectSingleNode("s3:Prefix", xmlNamespaceManager)?.InnerText;
                var entryType = (path.EndsWith("/") ? EntryType.Directory : EntryType.File);
                DateTime created = System.Convert.ToDateTime(null);
                DateTime modified = System.Convert.ToDateTime(null);
                long length = 0;
                string etag = "";
                path = PathUtils.Uncombine(mBasePath, path);
                if (path.EndsWith("/")) {
                    path = path.Substring(0, path.Length - 1);
                }
                int flags = 0;
                return new Entry(path, entryType, created, modified, length, etag, flags);
            }
            throw new NotImplementedException();
        }
    }

}


