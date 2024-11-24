
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

using System;
using System.Linq;

namespace DProjects.Fs.Aws {

    [Protocol("s3", "")]
    [ProtocolUsage("s3://ACCESSKEYID:SECRETACCESSKEY@BUCKET.s3-REGION.amazonaws.com/[?autoGzip=true][&autoCache=true]")]
    [ProtocolExample("s3://ABCDEFGIIASDASD:12345355625436@my-bucket.s3-us‑east‑2.amazonaws.com", "")]
    public class FilesystemS3Factory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var accessKeyId = UrlUtils.UrlDecode(url.UserInfo.Split(':')[0]);
            var secretAccesKey = UrlUtils.UrlDecode((url.UserInfo + ":").Split(':')[1]);
            var aux = url.Host.Split('.');
            var bucket = aux[0];
            var region = aux[1].Substring(3);
            var basePath = url.AbsolutePath;
            var isReadonly = false;
            var autoGzip = false;
            var autoCache = false;
            autoGzip = UrlUtils.GetQueryValue<bool>(url.Query, "autoGzip", true);
            autoCache = UrlUtils.GetQueryValue<bool>(url.Query, "autoCache", true);
            var uploadPartSize = 50 * 1024 * 1024;

            return new FilesystemS3(bucket, region, accessKeyId, secretAccesKey, basePath, autoGzip, autoCache, isReadonly, uploadPartSize);
        }

    }
     
}
