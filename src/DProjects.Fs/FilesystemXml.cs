using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Threading.Tasks;

using DProjects.Utils;
using DProjects.Fs.Extensions;
using DProjects.Streams;
using System.Threading;
using DProjects.Crypto;

namespace DProjects.Fs {


    public class FilesystemXml : FilesystemSync {


        //variables
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly XmlDocument mDocument;
        private readonly bool mInit;
        private readonly bool mAutoFlush;
        private readonly bool mGzip;
        private readonly bool mIndent;
        private readonly string? mPassword;

        private bool mDirty;


        //constructor
        public FilesystemXml(IFilesystem filesystem, string path, bool isReadonly, bool init, bool autoFlush, bool gzip, string? password, bool indent) : base(isReadonly) {
            mFilesystem = filesystem;
            mPath = path;
            mAutoFlush = autoFlush;
            mGzip = gzip;
            mIndent = indent;
            mPassword = password;
            mInit = init;
            var entry = mFilesystem.GetEntry(path);
            if ((entry == null || entry.Length == 0) && init) {
                mDocument = new XmlDocument();
                var xmlDir = mDocument.CreateElement("dir");
                mDocument.AppendChild(xmlDir);
                Persist();
            }
            mDocument = Load();
        }
        public override void Dispose() {
            if (mDirty) {
                Persist();
            }
            base.Dispose();
        }


        //properties
        public override string Url {
            get {
                var parameters = new List<string>();
                if (mInit) parameters.Add("init=true");
                if (mAutoFlush) parameters.Add("autoFlush=true");
                if (mGzip) parameters.Add("gzip=true");
                if (mIndent) parameters.Add("indent=true");
                return "xml:" + mFilesystem.Url + (mPath != "/" ? mPath : "") + (parameters.Count > 0 ? "!?" + string.Join("&", parameters) : "");
            }
        }


        //methods
        public override Entry? GetEntry(string path) {
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNode == null) return null;
            return ToEntry(xmlNode, PathUtils.GetPathParent(path));
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNode == null) throw new Exception("Unable to load read stream: file not found: " + path);
            var entries = new List<Entry>();
            foreach (XmlElement xmlChildNode in xmlNode.ChildNodes) {
                if (xmlChildNode.Name.Equals("dir") || xmlChildNode.Name.Equals("file")) entries.Add(ToEntry(xmlChildNode, path));
            }
            entries.Sort(new EntryComparer());
            foreach (var entry in entries) {
                var isValid = false;
                if (entry.IsFile() && (mode == GetModes.All || mode == GetModes.Files || mode == GetModes.Descendants)) isValid = true;
                if (entry.IsDirectory() && (mode == GetModes.All || mode == GetModes.Directories || mode == GetModes.Descendants)) isValid = true;
                if (isValid) {
                    if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                        yield return entry;
                    }
                }
                if (mode == GetModes.Descendants && entry.IsDirectory()) {
                    foreach (var subentry in GetEntries(entry.Path, mode, pattern)) {
                        yield return subentry;
                    }
                }
            }
        }
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNode == null) throw new Exception("Unable to load read stream: file not found: " + path);
            if (xmlNode.Name.Equals("dir")) throw new Exception("Unable to load read stream: directory: " + path);
            var ms = new MemoryStream();
            foreach(XmlElement xmlNodeContent in xmlNode.SelectNodes("content")) {
                var encoding = xmlNodeContent.GetAttribute("encoding");
                if (String.IsNullOrEmpty(encoding)) {
                    var buffer = System.Text.Encoding.UTF8.GetBytes(xmlNodeContent.InnerText);
                    ms.Write(buffer, 0, buffer.Length);
                } else if (encoding.Equals("base64")) {
                    var buffer = Base64Utils.FromBase64(xmlNodeContent.InnerText);
                    ms.Write(buffer, 0, buffer.Length);
                } else {
                    throw new Exception("Unable to load read stream: invalid encoding: " + encoding + ", " + path);
                }

            }
            ms.Position = 0;
            //var xmlNodeContent = (XmlElement) xmlNode.SelectSingleNode("content");
            //var encoding = xmlNodeContent.GetAttribute("encoding");
            //if (String.IsNullOrEmpty(encoding)) {
            //    result = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlNodeContent.InnerText));
            //} else if (encoding.Equals("base64")) {
            //    result = new MemoryStream(Base64Utils.FromBase64(xmlNodeContent.InnerText));
            //} else {
            //    throw new Exception("Unable to load read stream: invalid encoding: " + encoding + ", " + path);
            //}
            Stream? result = ms;
            if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                result = new PartialInputStream(ms, settings.Offset, settings.Length);
            } 
            return result;
        }


        //methods LEVEL 2
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            if (!ExistsDirectory(PathUtils.GetPathParent(path))) throw new Exception("Unable to modify filesystem: parent path not found");
            PathUtils.Validate(path);
            var append = (settings != null && settings.Append);
            var pathParent = PathUtils.GetPathParent(path);
            var xmlNodeParent = GetXmlNodeByPath(pathParent);
            if (xmlNodeParent==null) throw new Exception("Unable to modify filesystem: parent path not found");
            var xmlNode = GetXmlNodeByPath(path);
            byte[] content = StreamUtils.ReadBytes(stream);
            if (xmlNode == null) {
                xmlNode = mDocument.CreateElement("file");
                xmlNode.SetAttribute("name", PathUtils.GetPathName(path));
                xmlNode.SetAttribute("created", DateTime.Now.ToString(DateTimeUtils.DATETIME_ISO8601_MS));
                xmlNodeParent.AppendChild(xmlNode);
            }
            var modified = DateTime.Now;
            xmlNode.SetAttribute("modified", modified.ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            xmlNode.SetAttribute("etag", HashUtils.ToHashSHA1Hex(content.Length + "-" + modified.ToUniversalTime().ToString("YYYY-MM-dd-HH-mm-ss")).ToLower());
            if (!append) {
                while (xmlNode.LastChild != null && xmlNode.LastChild.Name.Equals("content")) {
                    xmlNode.RemoveChild(xmlNode.LastChild);
                }
            } 
            var xmlNodeContent = mDocument.CreateElement("content");
            xmlNode.AppendChild(xmlNodeContent);
            if (!append) {
                xmlNode.SetAttribute("length", content.Length.ToString());
            } else {
                var aux = xmlNode.GetAttribute("length");
                var contentLength = (long)0;
                if (!string.IsNullOrEmpty(aux)) contentLength = long.Parse(aux);
                xmlNode.SetAttribute("length", (contentLength + content.Length).ToString());
            }
            xmlNodeContent.InnerText = Base64Utils.ToBase64(content);
            xmlNodeContent.SetAttribute("encoding", "base64");
            //var xmlNodeContent = (XmlElement?) xmlNode.SelectSingleNode("content");
            //if (xmlNodeContent == null) {
            //    xmlNodeContent = mDocument.CreateElement("content");
            //    xmlNode.AppendChild(xmlNodeContent);
            //}
            //if (append) {
            //    var current = StreamUtils.ReadBytes(LoadReadStream(path, new()));
            //    xmlNodeContent.InnerText = Base64Utils.ToBase64(ByteUtils.Concat(current, content));
            //    xmlNode.SetAttribute("length", (current.Length + content.Length).ToString());
            //} else {
            //    xmlNode.SetAttribute("length", content.Length.ToString());
            //    xmlNodeContent.InnerText = Base64Utils.ToBase64(content);
            //}
            //xmlNodeContent.SetAttribute("encoding", "base64");
            mDirty = true;
            if (mAutoFlush) Persist();            
            return GetEntry(path)!;
        }
        public override Entry CreateDirectory(string path) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            var pathParent = PathUtils.GetPathParent(path);
            if (!ExistsDirectory(pathParent)) CreateDirectory(pathParent);
            var xmlNodeParent = GetXmlNodeByPath(pathParent);
            if (xmlNodeParent != null) {
                var xmlNode = GetXmlNodeByPath(path);
                if (xmlNode == null) {
                    var xmlNodeDir = mDocument.CreateElement("dir");
                    xmlNodeDir.SetAttribute("name", PathUtils.GetPathName(path));
                    xmlNodeDir.SetAttribute("created", DateTime.Now.ToString(DateTimeUtils.DATETIME_ISO8601_MS));
                    xmlNodeDir.SetAttribute("modified", DateTime.Now.ToString(DateTimeUtils.DATETIME_ISO8601_MS));
                    xmlNodeDir.SetAttribute("etag", "");
                    xmlNodeDir.SetAttribute("length", 0.ToString());
                    xmlNodeParent.AppendChild(xmlNodeDir);
                    mDirty = true;
                    if (mAutoFlush) Persist();
                }
            }
            return GetEntry(path)!;
        }
        public override void Delete(string path) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            var pathParent = PathUtils.GetPathParent(path);
            var xmlNodeParent = GetXmlNodeByPath(pathParent);
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNodeParent!=null && xmlNode != null) {
                xmlNodeParent.RemoveChild(xmlNode);
                mDirty = true;
                if (mAutoFlush) Persist();
            }
        }
        public override void Touch(string path, DateTime aDate) {
            throw new NotSupportedException("Touch is not supported by the XML filesystem.");
        }

        
        //methods LEVEL 4
        public override IDictionary<string, string> GetMetadata(string path) {
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNode == null) throw new Exception("Unable to get metadata: path not found: " + path);
            var xmlNodeMeta = (XmlElement?)xmlNode.SelectSingleNode("meta");
            if (xmlNodeMeta == null) return new Dictionary<string, string>();
            var result = new Dictionary<string, string>();
            foreach (XmlElement xmlNodeMetaVariable in xmlNodeMeta.ChildNodes) {
                var key = xmlNodeMetaVariable.GetAttribute("key");
                var value = xmlNodeMetaVariable.GetAttribute("value");
                result[key] = value;
            }
            return result;
        }
        public override void SetMetadata(string path, IDictionary<string, string> metadata) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            var xmlNode = GetXmlNodeByPath(path);
            if (xmlNode == null) throw new Exception("Unable to set metadata: path not found: " + path);            
            var xmlNodeMeta = (XmlElement?)xmlNode.SelectSingleNode("meta");
            if (xmlNodeMeta == null) {
                xmlNodeMeta = mDocument.CreateElement("meta");
                xmlNode.InsertBefore(xmlNodeMeta, xmlNode.SelectSingleNode("content"));
            }
            while (xmlNodeMeta.ChildNodes.Count>0) {
                xmlNodeMeta.RemoveChild(xmlNodeMeta.ChildNodes[0]);
            }
            var addedKeys = new List<string>();
            foreach (var key in metadata.Keys) {
                var keyToUse = key.ToLower().Trim();
                var value = metadata[key];
                if (!addedKeys.Contains(keyToUse)) {
                    var xmlNodeMetaVariable = mDocument.CreateElement("variable");
                    xmlNodeMetaVariable.SetAttribute("key", keyToUse);
                    xmlNodeMetaVariable.SetAttribute("value", value);
                    xmlNodeMeta.AppendChild(xmlNodeMetaVariable);
                    addedKeys.Add(keyToUse);
                }
            }
            mDirty = true;
            if (mAutoFlush) Persist();
        }
        public override bool Supports(string path, Features feature) {
            if (feature == Features.Metadata) return true;
            return false;
        }

        //private methods
        private XmlElement? GetXmlNodeByPath(string path) {
            var xmlNode = mDocument.DocumentElement;
            if (!path.Equals("/")) {
                var pathParts = path.Split('/');
                for (var i = 1; i < pathParts.Length; i++) {
                    var pathPart = pathParts[i];
                    var bFound = false;
                    foreach (XmlElement xmlChildNode in xmlNode.ChildNodes) {
                        if (pathPart.Equals(xmlChildNode.GetAttribute("name"))) {
                            xmlNode = xmlChildNode;
                            bFound = true;
                        }
                    }
                    if (!bFound) return null;
                }
            }
            return xmlNode;
        }
        private Entry ToEntry(XmlElement xmlNode, string path) {
            return new Entry(
                (xmlNode.OwnerDocument.DocumentElement == xmlNode ? "/" : PathUtils.Combine(path, xmlNode.GetAttribute("name"))),
                (xmlNode.Name.Equals("dir") ? EntryType.Directory : EntryType.File),
                (string.IsNullOrEmpty(xmlNode.GetAttribute("created")) ? new DateTime() : DateTimeUtils.Parse(xmlNode.GetAttribute("created"))),
                (string.IsNullOrEmpty(xmlNode.GetAttribute("modified")) ? new DateTime() : DateTimeUtils.Parse(xmlNode.GetAttribute("modified"))),
                (string.IsNullOrEmpty(xmlNode.GetAttribute("length")) ? (long) 0 : long.Parse(xmlNode.GetAttribute("length"))),
                (string.IsNullOrEmpty(xmlNode.GetAttribute("etag")) ? "" : xmlNode.GetAttribute("etag")),
                (string.IsNullOrEmpty(xmlNode.GetAttribute("flags")) ? 0 : int.Parse(xmlNode.GetAttribute("flags")))
            );
        }
        private XmlDocument Load() {
            var buffer = mFilesystem.LoadBinaryFile(mPath);
            if (mGzip) buffer = GzipUtils.UnGzip(buffer);
            if (mPassword != null) {
                using (var crypto = new DProjects.Crypto.CryptoSymmetricDecryptAES()) {
                    buffer = crypto.Decrypt(buffer, mPassword);
                }
            }
            var xml = System.Text.Encoding.UTF8.GetString(buffer);
            return XmlUtils.LoadXml(xml);
        }
        private void Persist() {
            AsyncUtils.RunSync(() => PersistAsync(CancellationToken.None));
        }
        private async Task PersistAsync(CancellationToken cancellationToken) {
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Encoding = new UTF8Encoding(false);
            xmlWriterSettings.Indent = mIndent;
            var toXmlSettings = new FilesystemToXml.ToXmlSettings();
            toXmlSettings.Content = true;
            toXmlSettings.Base64Content = true;
            toXmlSettings.Metadata = true;
            using (var tmpStream = new MemoryStream()) {
                using (var xmlWriter = System.Xml.XmlWriter.Create(tmpStream, xmlWriterSettings)) {
                    await this.ToXmlAsync("/", xmlWriter, toXmlSettings, cancellationToken);
                }
                var buffer = tmpStream.ToArray();
                if (mGzip) buffer = GzipUtils.Gzip(buffer);
                if (mPassword != null) {
                    var options = new DProjects.Crypto.CryptoSymmetricEncryptAES.Options() {
                        Fold = 76
                    };
                    using (var crypto = new DProjects.Crypto.CryptoSymmetricEncryptAES(options)) {
                        buffer = crypto.Encrypt(buffer, mPassword);
                    }
                }
                await mFilesystem.SaveBinaryFileAsync(mPath, buffer, cancellationToken);
            }
            mDirty = false;
        }

    }

}
