using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;

namespace DProjects.Azure {


    public class Utils {

        //methods
        public static string UriEncode(string input, bool encodeSlash) {
            var result = new StringBuilder();
            for (int i = 0; i <= input.Length - 1; i++) {
                char ch = input[i];
                //"(", ")", "[", "]", "`", "{", "}", "~"
                if (('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || ch == '_' || ch == '-' || ch == '~' || ch == '.' || ch == '+' || ch == '&' || ch == '@' || ch == '=' || ch == ';' || ch == ',' || ch == '\'' || ch == '(' || ch == ')') {
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
        public static void SignRequestSharedKeyLite(HttpRequestMessage httpRequest, string pathAbsolute, string query, string accountName, byte[] accountKey) {
            //https://docs.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key
            //version
            httpRequest.Headers.Add("x-ms-version", "2020-02-10");
            //date header
            var xDate = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
            httpRequest.Headers.Add("x-ms-date", xDate);
            //canonicalizedHeaders 
            var headernames = new List<string>();
            foreach (var header in httpRequest.Headers) {
                if (header.Key.StartsWith("x-")) {
                    headernames.Add(header.Key.ToLowerInvariant());
                }
            }
            var headernamesSorted = headernames.ToArray();
            Array.Sort(headernamesSorted, (a, b) => {
                return a.Replace('-', 'z').CompareTo(b.Replace('-', 'z'));
            });
            var canonicalizedHeaders = new StringBuilder();
            foreach (var header in headernamesSorted) {
                canonicalizedHeaders.Append(header + ":" + String.Join(", ", httpRequest.Headers.GetValues(header)).Trim() + "\n");
            }
            //canonicalizedResource
            var canonicalizedResource = new StringBuilder();
            var queryString = UrlUtils.ParseQueryString(query);
            canonicalizedResource.Append("/" + accountName + Utils.UriEncode(pathAbsolute, false) + (queryString["comp"] != null ? "?comp=" + queryString["comp"] : ""));
            //string to sign
            var stringToSign = httpRequest.Method.ToString().ToUpper() + "\n" +
               "\n" + //Content-MD5
               (httpRequest.Content != null && httpRequest.Content.Headers.ContentType != null ? httpRequest.Content.Headers.ContentType : "") + "\n" + //Content-Type
               "\n" + //ifMatch
               canonicalizedHeaders.ToString() + //CanonicalizedHeaders +
               canonicalizedResource.ToString() + //CanonicalizedResource;
               "";
            var stringToSignUtf8 = System.Text.Encoding.UTF8.GetBytes(stringToSign);
            //sign
            var signature = HashUtils.ToHashHmacSha256(accountKey, stringToSignUtf8);
            var signatureBase64 = Base64Utils.ToBase64(signature);
            //add auth header
            httpRequest.Headers.Add(HttpUtils.HEADER_AUTHORIZATION, "SharedKeyLite " + accountName + ":" + signatureBase64);
        }
        public static void SignRequestSharedKey(HttpRequestMessage httpRequest, string pathPrefix, string pathAbsolute, string query, string accountName, byte[] accountKey) {
            //https://learn.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key
            //version
            httpRequest.Headers.Add("x-ms-version", "2009-09-19");
            //date header
            var xDate = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);
            httpRequest.Headers.Add("x-ms-date", xDate);
            //canonicalizedHeaders 
            var headerNames = new List<string>();
            foreach (var header in httpRequest.Headers) {
                if (header.Key.StartsWith("x-")) {
                    headerNames.Add(header.Key.ToLowerInvariant());
                }
            }
            var headerNamesSorted = headerNames.ToArray();
            Array.Sort(headerNamesSorted, (a, b) => {
                return a.Replace('-', 'z').CompareTo(b.Replace('-', 'z'));
            });
            var canonicalizedHeaders = new StringBuilder();
            foreach (var header in headerNamesSorted) {
                canonicalizedHeaders.Append(header + ":" + String.Join(", ", httpRequest.Headers.GetValues(header)).Trim() + "\n");
            }
            //canonicalizedResource
            var canonicalizedResource = new StringBuilder();
            canonicalizedResource.Append(pathPrefix + "/" + accountName + Utils.UriEncode(pathAbsolute, false));
            //query
            var queryString = UrlUtils.ParseQueryString(query);
            var queryKeys = new List<string>();
            foreach(string key in queryString.Keys) {
                queryKeys.Add(key.ToLowerInvariant());
            }
            queryKeys.Sort();
            foreach(var variable in queryKeys) {
                canonicalizedResource.Append("\n" + variable + ":" + queryString[variable]);
            }
            //string to sign
            var stringToSign = httpRequest.Method.ToString().ToUpper() + "\n" +
                (httpRequest.Content != null && httpRequest.Content.Headers.ContentEncoding != null ? String.Join(",",httpRequest.Content.Headers.ContentEncoding) : "") + "\n" + //ContentEncoding
                (httpRequest.Content != null && httpRequest.Content.Headers.ContentLanguage != null ? String.Join(",",httpRequest.Content.Headers.ContentLanguage) : "") + "\n" + //ContentLanguage
                (httpRequest.Content != null && httpRequest.Content.Headers.ContentLength != null ? String.Join(",", (httpRequest.Content.Headers.ContentLength == 0 ? "" : httpRequest.Content.Headers.ContentLength)) : "") + "\n" + //ContentLength
                (httpRequest.Content != null && httpRequest.Content.Headers.ContentMD5 != null ? httpRequest.Content.Headers.ContentMD5 : "") + "\n" + //Content-ContentMD5
                (httpRequest.Content != null && httpRequest.Content.Headers.ContentType != null ? httpRequest.Content.Headers.ContentType : "") + "\n" + //Content-Type
                "\n" + //Date
                "\n" + //ifModifiedSince
                "\n" + //ifMatch
                "\n" + //ifNoneMatch
                "\n" + //ifUnmodifiedSince
                "\n" + //Range
                canonicalizedHeaders.ToString() + //CanonicalizedHeaders +
                canonicalizedResource.ToString() + //CanonicalizedResource;
                "";
            var stringToSignUtf8 = System.Text.Encoding.UTF8.GetBytes(stringToSign);
            //sign
            var signature = HashUtils.ToHashHmacSha256(accountKey, stringToSignUtf8);
            var signatureBase64 = Base64Utils.ToBase64(signature);
            //add auth header
            httpRequest.Headers.Add(HttpUtils.HEADER_AUTHORIZATION, "SharedKey " + accountName + ":" + signatureBase64);
        }


    }

}


