
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using DProjects.Utils;

namespace DProjects.Queues {

    public class Message {

        //consts
        public const string HEADER_CONTENT_TYPE = "content-type";
        public const string HEADER_X_ID = "x-id";


        //ctor
        public Message() {
            Headers = new HeadersUtils.Headers();
            Body = new byte[] { };
        }
        public Message(byte[] body, HeadersUtils.Headers? headers = null) {
            Headers = headers ?? new HeadersUtils.Headers();
            Body = body;
        }
        public Message(string body, HeadersUtils.Headers? headers = null) {
            Headers = headers ?? new HeadersUtils.Headers();
            Headers.Set(HEADER_CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN + ";charset=utf-8");
            Body = System.Text.Encoding.UTF8.GetBytes(body);
        }


        //props
        public HeadersUtils.Headers Headers { get; }
        public string? ContentType {
            get => Headers.Get<string?>(HttpUtils.HEADER_CONTENT_TYPE, null);
            set => Headers.Set(HttpUtils.HEADER_CONTENT_TYPE, value);
        }
        public long? ContentLength {
            get => Headers.Get<long?>(HttpUtils.HEADER_CONTENT_LENGTH, null);
            set => Headers.Set(HttpUtils.HEADER_CONTENT_LENGTH, value);
        }
        public DateTime? Date {
            get => Headers.Get<DateTime?>(HttpUtils.HEADER_DATE, null);
            set => Headers.Set(HttpUtils.HEADER_DATE, value);
        }
        public byte[] Body { get; }


        //methods
        public bool BodyIsText() {
            return MimeTypeUtils.IsText(Headers.Get<string>(HEADER_CONTENT_TYPE, ""));
        }
        public string GetBodyAsString(Encoding? encoding = null) {
            if (encoding == null)                 encoding = Encoding.UTF8;
            var contentType = Headers.Get<string>(HEADER_CONTENT_TYPE, "");
            if (contentType.IndexOf("charset=") != -1) {
                string charset = contentType.Substring(contentType.IndexOf("charset=") + 8);
                if (charset.IndexOf("\"") != -1) {
                    charset = charset.Replace("\"", "");
                }
                encoding = Encoding.GetEncoding(charset);
            }
            return encoding.GetString(Body);
        }
        public override string ToString() {
            return GetBodyAsString();
        }

    }

}
