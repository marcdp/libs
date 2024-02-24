using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace DProjects.Utils {


    public static class HttpUtils {


        //class
        public class HttpHeaders : NameValueCollection {
            public T Get<T>(string name, T defaultValue) {
                var value = this.Get(name);
                if (value == null) return defaultValue;
                return ConvertUtils.To<T>(value);
            }
        }

        //ports
        public const int PORT_HTTP = 80;
        public const int PORT_HTTPS = 443;

        //cache
        public const string CACHE_CONTROL_PRIVATE = "private";
        public const string CACHE_CONTROL_PUBLIC = "public";
        public const string CACHE_CONTROL_NO_CACHE = "no-cache";
        public const string CACHE_CONTROL_NO_STORE = "no-store";

        //method
        public const string METHOD_GET = "GET";
        public const string METHOD_HEAD = "HEAD";
        public const string METHOD_POST = "POST";
        public const string METHOD_PUT = "PUT";
        public const string METHOD_DELETE = "DELETE";
        public const string METHOD_OPTIONS = "OPTIONS";

        //headers
        public const string HEADER_LAST_MODIFIED = "Last-Modified";
        public const string HEADER_IF_MODIFIED_SINCE = "If-Modified-Since";
        public const string HEADER_CONTENT_LENGTH = "Content-Length";
        public const string HEADER_TRANSFER_ENCODING = "Transfer-Encoding";
        public const string HEADER_CONTENT_TYPE = "Content-Type";
        public const string HEADER_EXPIRES = "Expires";
        public const string HEADER_CONTENT_MD5 = "Content-MD5";
        public const string HEADER_CONTENT_ENCODING = "Content-Encoding";
        public const string HEADER_CONTENT_DISPOSITION = "Content-Disposition";
        public const string HEADER_CONTENT_RANGE = "Content-Range";
        public const string HEADER_AUTHORIZATION = "Authorization";
        public const string HEADER_HOST = "Host";
        public const string HEADER_CONNECTION = "Connection";
        public const string HEADER_DATE = "Date";
        public const string HEADER_REFERER = "Referer";
        public const string HEADER_CACHE_CONTROL = "Cache-Control";
        public const string HEADER_CONTENT_TRANSFER_ENCODING = "Content-transfer-encoding";
        public const string HEADER_ACCEPT = "Accept";
        public const string HEADER_ACCEPT_ENCODING = "Accept-Encoding";
        public const string HEADER_ACCEPT_LANGUAGE = "Accept-Language";
        public const string HEADER_UPGRADE = "Upgrade";
        public const string HEADER_SEC_WEBSOCKET_PROTOCOL = "Sec-WebSocket-Protocol";
        public const string HEADER_SEC_WEBSOCKET_VERSION = "Sec-WebSocket-Version";
        public const string HEADER_SEC_WEBSOCKET_KEY = "Sec-WebSocket-key";
        public const string HEADER_SEC_WEBSOCKET_ACCEPT = "Sec-WebSocket-Accept";
        public const string HEADER_ETAG = "ETag";
        public const string HEADER_WWW_AUTHENTICATE = "WWW-Authenticate";
        public const string HEADER_ALLOW = "Allow";
        public const string HEADER_DAV = "Dav";
        public const string HEADER_USER_AGENT = "User-Agent";
        public const string HEADER_RANGE = "Range";
        public const string HEADER_COOKIE = "cookie";
        public const string HEADER_CONTENT_LANGUAGE = "content-language";
        public const string HEADER_LINK = "link";
        public const string HEADER_LOCATION = "location";
        public const string HEADER_ACCEPT_RANGES = "accept-ranges";
        public const string HEADER_SET_COOKIE = "set-cookie";


        //status
        public const int HTTP_CONTINUE = 100;
        public const int HTTP_SWITCHING_PROTOCOL = 101;
        public const int HTTP_OK = 200;
        public const int HTTP_CREATED = 201;
        public const int HTTP_ACCEPTED = 202;
        public const int HTTP_NON_AUTHORITATIVE_INFORMATION = 203;
        public const int HTTP_NO_CONTENT = 204;
        public const int HTTP_RESET_CONTENT = 205;
        public const int HTTP_PARTIAL_CONTENT = 206;
        public const int HTTP_MULTISTATUS = 207;
        public const int HTTP_MULTIPLE_CHOICE = 300;
        public const int HTTP_MOVED_PERMANENTLY = 301;
        public const int HTTP_FOUND = 302;
        public const int HTTP_SEE_OTHER = 303;
        public const int HTTP_NOT_MODIFIED = 304;
        public const int HTTP_USE_PROXY = 305;
        public const int HTTP_UNUSED = 306;
        public const int HTTP_TEMPORARY_REDIRECT = 307;
        public const int HTTP_PERMANENT_REDIRECT = 308;
        public const int HTTP_BAD_REQUEST = 400;
        public const int HTTP_UNAUTHORIZED = 401;
        public const int HTTP_PAYMENT_REQUIRED = 402;
        public const int HTTP_FORBIDDEN = 403;
        public const int HTTP_NOT_FOUND = 404;
        public const int HTTP_METHOD_NOT_ALLOWED = 405;
        public const int HTTP_NOT_ACCEPTABLE = 406;
        public const int HTTP_PROXY_AUTHENTICATION_REQUIRED = 407;
        public const int HTTP_REQUEST_TIMEOUT = 408;
        public const int HTTP_CONFLICT = 409;
        public const int HTTP_GONE = 410;
        public const int HTTP_LENGTH_REQUIRED = 411;
        public const int HTTP_PRECONDITION_FAILED = 412;
        public const int HTTP_REQUEST_ENTITY_TOO_LARGE = 413;
        public const int HTTP_REQUEST_URI_TOO_LONG = 414;
        public const int HTTP_UNSUPPORTED_MEDIA_TYPE = 415;
        public const int HTTP_REQUESTED_RANGE_NOT_SATISFIABLE = 416;
        public const int HTTP_EXPECTATION_FAILED = 417;
        public const int HTTP_INTERNAL_SERVER_ERROR = 500;
        public const int HTTP_NOT_IMPLEMENTED = 501;
        public const int HTTP_BAD_GATEWAY = 502;
        public const int HTTP_SERVICE_UNAVAILABLE = 503;
        public const int HTTP_GATEWAY_TIMEOUT = 504;
        public const int HTTP_HTTP_VERSION_NOT_SUPPORTED = 505;


        //methods
        public static string GetHttpCodeDescription(int code, bool avoidPrefixAndCode = false) {
            var prefix = "Status Code: ";
            var separator = "; ";
            if (code == 100) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Continue";
            }
            if (code == 101) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Switching Protocol";
            }
            if (code == 200) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "OK";
            }
            if (code == 201) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Created";
            }
            if (code == 202) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Accepted";
            }
            if (code == 203) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Non-Authoritative Information";
            }
            if (code == 204) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "No Content";
            }
            if (code == 205) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Reset Content";
            }
            if (code == 206) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Partial Content";
            }
            if (code == 300) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Multiple Choice";
            }
            if (code == 301) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Moved Permanently";
            }
            if (code == 302) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Found";
            }
            if (code == 303) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "See Other";
            }
            if (code == 304) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Not Modified";
            }
            if (code == 305) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Use Proxy";
            }
            if (code == 306) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "unused";
            }
            if (code == 307) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Temporary Redirect";
            }
            if (code == 308) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Permanent Redirect";
            }
            if (code == 400) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Bad Request";
            }
            if (code == 401) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Unauthorized";
            }
            if (code == 402) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Payment Required";
            }
            if (code == 403) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Forbidden";
            }
            if (code == 404) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Not Found";
            }
            if (code == 405) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Method Not Allowed";
            }
            if (code == 406) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Not Acceptable";
            }
            if (code == 407) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Proxy Authentication Required";
            }
            if (code == 408) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Request Timeout";
            }
            if (code == 409) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Conflict";
            }
            if (code == 410) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Gone";
            }
            if (code == 411) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Length Required";
            }
            if (code == 412) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Precondition Failed";
            }
            if (code == 413) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Request Entity Too Large";
            }
            if (code == 414) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Request-URI Too Long";
            }
            if (code == 415) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Unsupported Media Type";
            }
            if (code == 416) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Requested Range Not Satisfiable";
            }
            if (code == 417) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Expectation Failed";
            }
            if (code == 500) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Internal Server Error";
            }
            if (code == 501) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Not Implemented";
            }
            if (code == 502) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Bad Gateway";
            }
            if (code == 503) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Service Unavailable";
            }
            if (code == 504) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Gateway Timeout";
            }
            if (code == 505) {
                return (!avoidPrefixAndCode ? prefix + code + separator : "") + "HTTP Version Not Supported";
            }
            return (!avoidPrefixAndCode ? prefix + code + separator : "") + "Unknow";
        }
        
    }

}


