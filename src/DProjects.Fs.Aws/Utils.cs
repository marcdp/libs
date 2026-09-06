using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

using DProjects.Utils;

namespace DProjects.Fs.Aws {


    public class Utils {


        //methods
        public static string UriEncode(string input, bool encodeSlash) {
            var result = new StringBuilder();
            for (int i = 0; i <= input.Length - 1; i++) {
                char ch = input[i];
                if (('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || ch == '_' || ch == '-' || ch == '~' || ch == '.') {
                    result.Append(ch);
                } else if (ch == '/') {
                    result.Append((encodeSlash ? "%2F" : "" + ch));
                } else {
                    foreach (byte myByte in System.Text.Encoding.UTF8.GetBytes("" + ch)) {
                        result.Append("%" + myByte.ToString("X2"));
                    }
                }
            }
            return result.ToString();
        }
        public static void SignRequestV4(HttpRequestMessage httpRequest, string region, string service, string host, string path, string query, string accessKeyId, string secretAccessKey, string? contentHasSha256 = null, string[]? canonicalHeaderNames = null) {
            var dateToUse = DateTime.Now.ToUniversalTime();
            contentHasSha256 = (contentHasSha256 == null ? "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" : contentHasSha256.ToLower());
            //TODO: process payload
            httpRequest.Headers.Add("x-amz-content-sha256", contentHasSha256);
            httpRequest.Headers.Add("x-amz-date", dateToUse.ToString("yyyyMMddTHHmmssZ"));
            //canonical uri
            string canonicalURI = UriEncode(path, false);
            //canonicalQueryString
            string canonicalQueryString = "";
            if (query.Length > 0) {
                var aux = new StringBuilder();
                var qskeys = new List<string>();
                var qskeys2 = new Dictionary<string, string>();
                foreach (string qspart in query.Substring(1).Split('&')) {
                    var qspartToUse = UrlUtils.UrlDecode(qspart);
                    string qsname = qspartToUse;
                    string qsvalue = "";
                    if (qspartToUse.IndexOf("=") != -1) {
                        qsvalue = qsname.Substring(qsname.IndexOf("=") + 1);
                        qsname = qsname.Substring(0, qsname.IndexOf("="));
                    }
                    qskeys2[qsname] = qsvalue;
                    qskeys.Add(qsname);
                }
                qskeys.Sort();
                foreach (string qsname in qskeys) {
                    string qsvalue = qskeys2[qsname];
                    if (aux.Length > 0) {
                        aux.Append("&");
                    }
                    aux.Append(UriEncode(qsname, true)).Append("=").Append(UriEncode(qsvalue, true));
                }
                canonicalQueryString = aux.ToString();
            }
            //canonical headers
            string canonicalHeaders = "";
            var headernames = new List<string>();
            if (canonicalHeaderNames != null) {
                headernames.AddRange(canonicalHeaderNames);
            } else {
                headernames.Add(HttpUtils.HEADER_HOST.ToLower());
                if (httpRequest.Content != null && httpRequest.Content!.Headers.Contains(HttpUtils.HEADER_CONTENT_TYPE)) headernames.Add(HttpUtils.HEADER_CONTENT_TYPE.ToLower());
                if (httpRequest.Content != null && httpRequest.Content!.Headers.Contains(HttpUtils.HEADER_CONTENT_MD5)) headernames.Add(HttpUtils.HEADER_CONTENT_MD5.ToLower());
                foreach (var header in httpRequest.Headers) {
                    if (header.Key.ToLower().StartsWith("x-amz-")) {
                        headernames.Add(header.Key.ToLower());
                    }
                }
            }
            //headernames.Sort();
            var headernamesSorted = headernames.ToArray();
            Array.Sort(headernamesSorted, StringComparer.OrdinalIgnoreCase);
            if (headernamesSorted.Length > 0) {
                var aux = new StringBuilder();
                foreach (string key in headernamesSorted) {
                    if (key.Equals("host")) {
                        aux.Append(key).Append(":").Append(host.Trim()).Append(CharUtils.CHAR_LF);
                    } else if (key.Equals("content-type")) {
                        var auxxx = httpRequest.Content!.Headers.GetValues(key);
                        aux.Append(key).Append(":").Append(httpRequest.Content.Headers.ContentType.ToString().Trim()).Append(CharUtils.CHAR_LF);
                    } else if (key.Equals("content-md5")) {
                        var auxxx = httpRequest.Content!.Headers.GetValues(key);
                        var buffer = httpRequest.Content.Headers.ContentMD5;
                        aux.Append(key).Append(":").Append(Base64Utils.ToBase64(buffer)).Append(CharUtils.CHAR_LF);
                    } else {
                        var value = new List<string>(httpRequest.Headers.GetValues(key));
                        var value_str = string.Join(", ", value.ToArray());
                        //foreach (var valueTarget in httpRequest.Headers.GetValues(key)) {
                        //    value = valueTarget;
                        //    break;
                        //}
                        aux.Append(key).Append(":").Append(value_str.Trim()).Append(CharUtils.CHAR_LF);
                    }
                }
                canonicalHeaders = aux.ToString();
            }
            //signed headers
            string signedHeaders = "";
            if (headernamesSorted.Length > 0) {
                StringBuilder aux = new StringBuilder();
                foreach (string key in headernamesSorted) {
                    if (aux.Length > 0) {
                        aux.Append(";");
                    }
                    aux.Append(key);
                }
                signedHeaders = aux.ToString();
            }
            //canonical request
            string canonicalRequest = "" +
                httpRequest.Method.ToString() + CharUtils.CHAR_LF +
                canonicalURI + CharUtils.CHAR_LF +
                canonicalQueryString + CharUtils.CHAR_LF +
                canonicalHeaders + CharUtils.CHAR_LF +
                signedHeaders + CharUtils.CHAR_LF +
                contentHasSha256;
            //stringtoSign
            string timeStampISO8601Format = dateToUse.ToString("yyyyMMddTHHmmssZ");
            string scope = (dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request").ToLower();
            string stringToSign = "AWS4-HMAC-SHA256" + CharUtils.CHAR_LF +
                timeStampISO8601Format + CharUtils.CHAR_LF +
                scope + CharUtils.CHAR_LF +
                ConvertUtils.ToHexString(HashUtils.ToHashSHA256(canonicalRequest)).ToLower();
            //signingKey
            byte[] dateKey = HashUtils.ToHashHmacSha256("AWS4" + secretAccessKey, dateToUse.ToString("yyyyMMdd"));
            byte[] dateRegionKey = HashUtils.ToHashHmacSha256(dateKey, region);
            byte[] dateRegionServiceKey = HashUtils.ToHashHmacSha256(dateRegionKey, service);
            byte[] signingKey = HashUtils.ToHashHmacSha256(dateRegionServiceKey, "aws4_request");
            string signature = ConvertUtils.ToHexString(HashUtils.ToHashHmacSha256(signingKey, stringToSign)).ToLower();
            //credential
            string credential = accessKeyId + "/" + dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request";
            //set header
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("AWS4-HMAC-SHA256", "Credential=" + credential + ", SignedHeaders=" + signedHeaders + ", Signature=" + signature);
        }
        public static void SignRequestV4(HttpWebRequest httpWebRequest, string region, string service, string host, string path, string query, string accessKeyId, string secretAccessKey, string? contentHasSha256 = null, string[]? canonicalHeaderNames = null) {
            //classic HttpWebRequest
            var dateToUse = DateTime.Now.ToUniversalTime();
            contentHasSha256 = (contentHasSha256 == null ? "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" : contentHasSha256.ToLower());
            //TODO: process payload
            httpWebRequest.Headers.Add("x-amz-content-sha256", contentHasSha256);
            httpWebRequest.Headers.Add("x-amz-date", dateToUse.ToString("yyyyMMddTHHmmssZ"));
            //canonical uri
            string canonicalURI = UriEncode(path, false);
            //canonicalQueryString
            string canonicalQueryString = "";
            if (query.Length > 0) {
                var aux = new StringBuilder();
                var qskeys = new List<string>();
                var qskeys2 = new Dictionary<string, string>();
                foreach (string qspart in query.Substring(1).Split('&')) {
                    var qspartToUse = UrlUtils.UrlDecode(qspart);
                    string qsname = qspartToUse;
                    string qsvalue = "";
                    if (qspartToUse.IndexOf("=") != -1) {
                        qsvalue = qsname.Substring(qsname.IndexOf("=") + 1);
                        qsname = qsname.Substring(0, qsname.IndexOf("="));
                    }
                    qskeys2[qsname] = qsvalue;
                    qskeys.Add(qsname);
                }
                qskeys.Sort();
                foreach (string qsname in qskeys) {
                    string qsvalue = qskeys2[qsname];
                    if (aux.Length > 0) {
                        aux.Append("&");
                    }
                    aux.Append(UriEncode(qsname, true)).Append("=").Append(UriEncode(qsvalue, true));
                }
                canonicalQueryString = aux.ToString();
            }
            //canonical headers
            string canonicalHeaders = "";
            var headernames = new List<string>();
            if (canonicalHeaderNames != null) {
                headernames.AddRange(canonicalHeaderNames);
            } else {
                headernames.Add(HttpUtils.HEADER_HOST.ToLower());
                //if (httpWebRequest.ContentType != null) headernames.Add(HttpUtils.HEADER_CONTENT_TYPE.ToLower());
                //if (httpWebRequest.ContentType != null && httpWebRequest.ContentType!.Headers.Contains(HttpUtils.HEADER_CONTENT_TYPE)) headernames.Add(HttpUtils.HEADER_CONTENT_TYPE.ToLower());
                //if (httpWebRequest.Content != null && httpRequest.Content!.Headers.Contains(HttpUtils.HEADER_CONTENT_TYPE)) headernames.Add(HttpUtils.HEADER_CONTENT_TYPE.ToLower());
                //if (httpRequest.Content != null && httpRequest.Content!.Headers.Contains(HttpUtils.HEADER_CONTENT_MD5)) headernames.Add(HttpUtils.HEADER_CONTENT_MD5.ToLower());
                foreach (var headerKey in httpWebRequest.Headers.Keys) {
                    if (headerKey.ToString().ToLower().StartsWith("x-amz-") || headerKey.ToString().ToLower().StartsWith("content-type") || headerKey.ToString().ToLower().StartsWith("content-md5")) {
                        headernames.Add(headerKey.ToString().ToLower());
                    }
                }
            }
            //headernames.Sort();
            var headernamesSorted = headernames.ToArray();
            Array.Sort(headernamesSorted, StringComparer.OrdinalIgnoreCase);
            if (headernamesSorted.Length > 0) {
                var aux = new StringBuilder();
                foreach (string key in headernamesSorted) {
                    if (key.Equals("host")) {
                        aux.Append(key).Append(":").Append(host.Trim()).Append(CharUtils.CHAR_LF);
                    } else if (key.Equals("content-type")) {
                        aux.Append(key).Append(":").Append(httpWebRequest.ContentType.ToString().Trim()).Append(CharUtils.CHAR_LF);
                    } else if (key.Equals("content-md5")) {
                    } else {
                        var value = new List<string>(httpWebRequest.Headers.GetValues(key));
                        var value_str = string.Join(", ", value.ToArray());
                        aux.Append(key).Append(":").Append(value_str.Trim()).Append(CharUtils.CHAR_LF);
                    }
                }
                canonicalHeaders = aux.ToString();
            }
            //signed headers
            string signedHeaders = "";
            if (headernamesSorted.Length > 0) {
                StringBuilder aux = new StringBuilder();
                foreach (string key in headernamesSorted) {
                    if (aux.Length > 0) {
                        aux.Append(";");
                    }
                    aux.Append(key);
                }
                signedHeaders = aux.ToString();
            }
            //canonical request
            string canonicalRequest = "" +
                httpWebRequest.Method.ToString() + CharUtils.CHAR_LF +
                canonicalURI + CharUtils.CHAR_LF +
                canonicalQueryString + CharUtils.CHAR_LF +
                canonicalHeaders + CharUtils.CHAR_LF +
                signedHeaders + CharUtils.CHAR_LF +
                contentHasSha256;
            //stringtoSign
            string timeStampISO8601Format = dateToUse.ToString("yyyyMMddTHHmmssZ");
            string scope = (dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request").ToLower();
            string stringToSign = "AWS4-HMAC-SHA256" + CharUtils.CHAR_LF +
                timeStampISO8601Format + CharUtils.CHAR_LF +
                scope + CharUtils.CHAR_LF +
                ConvertUtils.ToHexString(HashUtils.ToHashSHA256(canonicalRequest)).ToLower();
            //signingKey
            byte[] dateKey = HashUtils.ToHashHmacSha256("AWS4" + secretAccessKey, dateToUse.ToString("yyyyMMdd"));
            byte[] dateRegionKey = HashUtils.ToHashHmacSha256(dateKey, region);
            byte[] dateRegionServiceKey = HashUtils.ToHashHmacSha256(dateRegionKey, service);
            byte[] signingKey = HashUtils.ToHashHmacSha256(dateRegionServiceKey, "aws4_request");
            string signature = ConvertUtils.ToHexString(HashUtils.ToHashHmacSha256(signingKey, stringToSign)).ToLower();
            //credential
            string credential = accessKeyId + "/" + dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request";
            //set header
            httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, "AWS4-HMAC-SHA256 Credential=" + credential + ", SignedHeaders=" + signedHeaders + ", Signature=" + signature);
        }
        public static string PreSignUrlV4(string method, string aUrl, int expiresSeconds = 900) {
            //s3://...:...%2Fgx3EnEKGRIo37W@cett-activitats.s3-eu-west-3.amazonaws.com
            var url = new Uri(aUrl);
            var accessKeyId = UrlUtils.UrlDecode(url.UserInfo.Split(':')[0]);
            var secretAccessKey = UrlUtils.UrlDecode(url.UserInfo.Split(':')[1]);
            var aux = url.Host.Split('.');
            var bucket = aux[0];
            var region = aux[1].Substring(3);
            var basePath = url.AbsolutePath;
            var service = "s3";
            return PreSignUrlV4(method, region, service, url.Host, url.LocalPath, url.Query, accessKeyId, secretAccessKey, expiresSeconds);
        }
        public static string PreSignUrlV4(string method, string region, string service, string host, string path, string query, string accessKeyId, string secretAccessKey, int expiresSeconds) {
            var dateToUse = DateTime.Now.ToUniversalTime();
            //canonical uri
            string canonicalURI = UriEncode(path, false);
            //canonical headers
            string signedHeaders = "host";
            var canonicalHeaders = "host:" + host + CharUtils.CHAR_LF;
            //credential
            string credential = accessKeyId + "/" + dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request";
            //canonical queryString
            string canonicalQueryString = "";
            if (query.Length > 0) {
                var aux = new StringBuilder();
                var qskeys = new List<string>();
                var qskeys2 = new Dictionary<string, string>();
                foreach (string qspart in query.Substring(1).Split('&')) {
                    var qspartToUse = UrlUtils.UrlDecode(qspart);
                    string qsname = qspartToUse;
                    string qsvalue = "";
                    if (qspartToUse.IndexOf("=") != -1) {
                        qsvalue = qsname.Substring(qsname.IndexOf("=") + 1);
                        qsname = qsname.Substring(0, qsname.IndexOf("="));
                    }
                    qskeys2[qsname] = qsvalue;
                    qskeys.Add(qsname);
                }
                qskeys.Sort();
                foreach (string qsname in qskeys) {
                    string qsvalue = qskeys2[qsname];
                    if (aux.Length > 0) {
                        aux.Append("&");
                    }
                    aux.Append(UriEncode(qsname, true)).Append("=").Append(UriEncode(qsvalue, true));
                }
                canonicalQueryString = aux.ToString();
            }
            canonicalQueryString += (canonicalQueryString.Length > 0 ? "&" : "") +
                "X-Amz-Algorithm=AWS4-HMAC-SHA256" +
                "&X-Amz-Credential=" + credential.Replace("/", "%2F") +
                "&X-Amz-Date=" + dateToUse.ToString("yyyyMMddTHHmmssZ") +
                "&X-Amz-Expires=" + expiresSeconds +
                "&X-Amz-SignedHeaders=" + signedHeaders;
            //canonical request
            string canonicalRequest = "" +
                method.ToString() + CharUtils.CHAR_LF +
                canonicalURI + CharUtils.CHAR_LF +
                canonicalQueryString + CharUtils.CHAR_LF +
                canonicalHeaders + CharUtils.CHAR_LF +
                signedHeaders + CharUtils.CHAR_LF +
                "UNSIGNED-PAYLOAD";
            //stringtoSign
            string timeStampISO8601Format = dateToUse.ToString("yyyyMMddTHHmmssZ");
            string scope = (dateToUse.ToString("yyyyMMdd") + "/" + region + "/" + service + "/aws4_request").ToLower();
            string stringToSign = "AWS4-HMAC-SHA256" + CharUtils.CHAR_LF +
                timeStampISO8601Format + CharUtils.CHAR_LF +
                scope + CharUtils.CHAR_LF +
                ConvertUtils.ToHexString(HashUtils.ToHashSHA256(canonicalRequest)).ToLower();
            //signingKey
            byte[] dateKey = HashUtils.ToHashHmacSha256("AWS4" + secretAccessKey, dateToUse.ToString("yyyyMMdd"));
            byte[] dateRegionKey = HashUtils.ToHashHmacSha256(dateKey, region);
            byte[] dateRegionServiceKey = HashUtils.ToHashHmacSha256(dateRegionKey, service);
            byte[] signingKey = HashUtils.ToHashHmacSha256(dateRegionServiceKey, "aws4_request");
            string signature = ConvertUtils.ToHexString(HashUtils.ToHashHmacSha256(signingKey, stringToSign)).ToLower();
            //build url
            var result = new StringBuilder();
            var url = "https://" + host + path;
            result.Append(url);
            result.Append("?X-Amz-Algorithm=AWS4-HMAC-SHA256");
            result.Append("&X-Amz-Credential=" + credential.Replace("/", "%2F"));
            result.Append("&X-Amz-Date=" + dateToUse.ToString("yyyyMMddTHHmmssZ"));
            result.Append("&X-Amz-Expires=" + expiresSeconds);
            result.Append("&X-Amz-SignedHeaders=" + signedHeaders);
            result.Append("&X-Amz-Signature=" + signature);
            return result.ToString();
        }
    }

}


