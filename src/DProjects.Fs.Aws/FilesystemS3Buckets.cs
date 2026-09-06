using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using DProjects.Utils;

namespace DProjects.Fs.Aws {


    public class FilesystemS3Buckets : FilesystemRepository {

         
        //variables
        private string mRegion;
        private string mAccesKeyId;
        private string mSecretAccessKey;
        private string mPrefix;
        private string mSuffix;
        private bool mAutoGzip;
        private bool mAutoCache;
        private string mCostTag;
        private bool mAdmin;
        private IDictionary<string, string> mBucketRegions;


        //constructor
        public FilesystemS3Buckets(string region, string accessKeyId, string secretAccessKey, string prefix, string suffix, bool autoGzip, bool autoCache, string costTag, bool admin, IDictionary<string, string> bucketRegions, bool isReadOnly) : base(new MyRepository(region, accessKeyId, secretAccessKey, prefix, suffix, autoGzip, autoCache, costTag, admin, bucketRegions, isReadOnly), isReadOnly) {
            mRegion = region;
            mAccesKeyId = accessKeyId;
            mSecretAccessKey = secretAccessKey;
            mPrefix = prefix;
            mSuffix = suffix;
            mCostTag = costTag;
            mAdmin = admin;
            mBucketRegions = bucketRegions;
            mAutoGzip = autoGzip;
            mAutoCache = autoCache;
        }
        public override void Dispose() {
            base.Dispose();
        }


        //properties
        public override string Url {
            get {
                var query = new List<string>();
                if (mAutoCache) query.Add("autoCache=true");
                if (mAutoGzip) query.Add("autoGzip=true");
                if (!string.IsNullOrEmpty(mPrefix)) query.Add("prefix=" + mPrefix);
                if (!string.IsNullOrEmpty(mSuffix)) query.Add("suffix=" + mSuffix);
                if (!string.IsNullOrEmpty(mCostTag)) query.Add("cost-tag=" + mCostTag);
                if (mAdmin) query.Add("admin=true");
                foreach (var key in mBucketRegions.Keys) {
                    query.Add(key + ".region=" + mBucketRegions[key]);
                }
                //return "s3-buckets://" + UrlUtils.UrlEncode(mAccesKeyId) + ":" + UrlUtils.UrlEncode(mSecretAccessKey) + "@s3" + (!string.IsNullOrEmpty(mRegion) ? "-" + mRegion : "") + ".amazonaws.com/" + (query.Count > 0 ? "?" + string.Join("&", query.ToArray()) : "");
                return "s3-buckets://" + UrlUtils.UrlEncode(mAccesKeyId) + "@s3" + (!string.IsNullOrEmpty(mRegion) ? "-" + mRegion : "") + ".amazonaws.com/" + (query.Count > 0 ? "?" + string.Join("&", query.ToArray()) : "");
            }
        }


        //repo
        public class MyRepository : Repository, RepositoryWritable {

            //variables
            private string mRegion;
            private string mAccesKeyId;
            private string mSecretAccessKey;
            private string mService;

            private bool mAutoGzip;
            private bool mAutoCache;
            private string mPrefix;
            private string mSuffix;
            private string mCostTag;
            private bool mAdmin;
            private IDictionary<string, string> mBucketRegions;

            private bool mIsReadOnly;
            private HttpClientHandler mHttpClientHandler;
            private HttpClient mHttpClient;


            //constructor
            public MyRepository(string region, string accessKeyId, string secretAccessKey, string prefix, string suffix, bool autoGzip, bool autoCache, string costTag, bool admin, IDictionary<string, string> bucketRegions, bool isReadOnly) {
                mRegion = region;
                mAccesKeyId = accessKeyId;
                mSecretAccessKey = secretAccessKey;
                mService = "s3";
                mAutoGzip = autoGzip;
                mAutoCache = autoCache;
                mPrefix = prefix;
                mSuffix = suffix;
                mCostTag = costTag;
                mAdmin = admin;
                mBucketRegions = bucketRegions;
                mIsReadOnly = isReadOnly;
                mHttpClientHandler = new HttpClientHandler();
                mHttpClient = new HttpClient(mHttpClientHandler);
                mHttpClient.BaseAddress = new Uri("https://s3" + (!string.IsNullOrEmpty(region) ? "-" + region : "") + ".amazonaws.com");
                mHttpClient.Timeout = TimeSpan.FromDays(1);
            }

            //methods
            public async Task<Entry?> GetByIdAsync(string id, CancellationToken cancellationToken) {
                await foreach (var entry in GetByPatternAsync(null, cancellationToken)) {
                    if (entry.Name.Equals(id)) return entry;
                }
                return null;
            }
            public async IAsyncEnumerable<Entry> GetByPatternAsync(string? pattern, [EnumeratorCancellation] CancellationToken cancellationToken) {
                var httpRequest = CreateHttpRequest(HttpMethod.Get, "/", "");
                SignRequest(httpRequest, "/", "");
                //using (var httpResponse = await mHttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead)) {
                using (var httpResponse = await mHttpClient.SendAsync(httpRequest, cancellationToken)) {
                    var xml = await httpResponse.Content.ReadAsStringAsync();
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("Unable to get entries: " + httpResponse.StatusCode);
                    var xmlDocument = XmlUtils.LoadXml(xml);
                    var xmlDocumentElement = xmlDocument.DocumentElement;
                    if (xmlDocumentElement != null) {
                        var xlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                        xlNamespaceManager.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                        var xmlNodes = xmlDocumentElement.SelectNodes("//s3:Bucket", xlNamespaceManager);
                        if (xmlNodes != null) {
                            foreach (XmlNode? xmlNode in xmlNodes) {
                                if (xmlNode != null) {
                                    var name = UnPreSuffixName(XmlUtils.GetXmlChildNodeAs<string>(xmlNode, "Name", ""));
                                    if (name != null) {
                                        var creationDate = XmlUtils.GetXmlChildNodeAs<DateTime>(xmlNode, "CreationDate", default(DateTime));
                                        var entry = new Entry("/" + name, EntryType.Directory, creationDate, creationDate, 0, "", 0);
                                        if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                            yield return entry;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            public async Task<Entry> AddAsync(string id, CancellationToken cancellationToken) {
                if (mIsReadOnly) throw new InvalidOperationException("Unable to add bucket: filesystem is readonly");
                if (!mAdmin) throw new InvalidOperationException("Unable to add bucket: administrative access is required");
                var name = PreSuffixName(id);
                var fs = new FilesystemS3(name, mRegion, mAccesKeyId, mSecretAccessKey, "/", mAutoGzip, mAutoCache, mIsReadOnly, mHttpClientHandler);
                await fs.CreateBucketAsync(cancellationToken);
                if (!string.IsNullOrEmpty(mCostTag)) {
                    var tags = new Dictionary<string, string>();
                    tags[mCostTag] = name;
                    await fs.SetBucketTagsAsync(tags, cancellationToken);
                }
                return fs.GetEntry("/")!;
            }
            public async Task RemoveAsync(string id, CancellationToken cancellationToken) {
                if (mIsReadOnly) throw new InvalidOperationException("Unable to remove bucket: filesystem is readonly");
                if (!mAdmin) throw new InvalidOperationException("Unable to remove bucket: administrative access is required");
                var name = PreSuffixName(id);
                var fs = new FilesystemS3(name, mRegion, mAccesKeyId, mSecretAccessKey, "/", mAutoGzip, mAutoCache, mIsReadOnly, mHttpClientHandler);
                await fs.RemoveBucketAsync(false, cancellationToken);
            }
            public IFilesystem CreateFilesystem(string id, bool isReadonly) {
                var name = PreSuffixName(id);
                var region = mRegion;
                if (mBucketRegions.TryGetValue(name, out string? regionForced)) {
                    region = regionForced;
                }
                var fs = new FilesystemS3(name, region, mAccesKeyId, mSecretAccessKey, "/", mAutoGzip, mAutoCache, isReadonly, mHttpClientHandler);
                fs.Start();
                return fs;
            }


            //utils
            private string? UnPreSuffixName(string name) {
                if (!string.IsNullOrEmpty(mPrefix)) {
                    if (name.StartsWith(mPrefix)) {
                        name = name.Substring(mPrefix.Length);
                    } else {
                        return null;
                    }
                }
                if (!string.IsNullOrEmpty(mSuffix)) {
                    if (name.EndsWith(mSuffix)) {
                        name = name.Substring(0, name.Length - mSuffix.Length);
                    } else {
                        return null;
                    }
                }
                return name;
            }
            private string PreSuffixName(string name) {
                if (!string.IsNullOrEmpty(mPrefix)) name = mPrefix + name;
                if (!string.IsNullOrEmpty(mSuffix)) name = name + mSuffix;
                return name;
            }
            private HttpRequestMessage CreateHttpRequest(HttpMethod method, string pathAbsolute, string querystring) {
                var requestUri = new Uri(Utils.UriEncode(pathAbsolute, false) + querystring, UriKind.Relative);
                return new HttpRequestMessage(method, requestUri);
            }
            private void SignRequest(HttpRequestMessage httpRequest, string pathAbsolute, string query, string? contentHasSha256 = null) {
                Utils.SignRequestV4(httpRequest, mRegion, mService, mHttpClient.BaseAddress?.Host ?? "", pathAbsolute, query, mAccesKeyId, mSecretAccessKey, contentHasSha256);
            }
        }


    }

}


