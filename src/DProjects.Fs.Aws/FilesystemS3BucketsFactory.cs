
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Collections.Generic;

namespace DProjects.Fs.Aws {

    [Protocol("s3-buckets", "")]
    [ProtocolUsage("s3-buckets://ACCESSKEYID:SECRETACCESSKEY@BUCKET.s3-REGION.amazonaws.com/[?autoGzip=true][&autoCache=true]")]
    [ProtocolExample("s3-buckets://ABCDEFGIIASDASD:12345355625436@my-bucket.s3-us‑east‑2.amazonaws.com", "")]
    public class FilesystemS3BucketsFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var accessKeyId = UrlUtils.UrlDecode(url.UserInfo.Split(':')[0]);
            var secretAccessKey = url.UserInfo.IndexOf(":") != -1 ? UrlUtils.UrlDecode(url.UserInfo.Split(':')[1]) : "";
            var aux = url.Host.Split('.');
            var region = aux[0].Substring(3);

            var basePath = url.AbsolutePath;
            var prefix = UrlUtils.GetQueryValue<string>(url.Query, "prefix", "");
            var suffix = UrlUtils.GetQueryValue<string>(url.Query, "suffix", "");
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly");
            var costTag = UrlUtils.GetQueryValue<string>(url.Query, "cost-tag", "");
            var admin = UrlUtils.GetQueryValue<bool>(url.Query, "admin", false);
            var autoGzip = false;
            var autoCache = false;
            autoGzip = UrlUtils.GetQueryValue<bool>(url.Query, "autoGzip", true);
            autoCache = UrlUtils.GetQueryValue<bool>(url.Query, "autoCache", true);

            var bucketRegions = new Dictionary<string, string>();   
            var parameters  = UrlUtils.ParseQueryString(url.Query);
            foreach (var key in parameters.Keys) {
                if (key.ToString().EndsWith(".region")) {
                    var bucket_name = key.ToString().Split('.')[0];
                    var bucket_region = parameters.Get(key.ToString());
                    bucketRegions.Add(bucket_name, bucket_region);
                }
            }

            return new FilesystemS3Buckets(region, accessKeyId, secretAccessKey,  prefix, suffix, autoGzip, autoCache, costTag, admin, bucketRegions, isReadonly);
        }

    }
     
}
