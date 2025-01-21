using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace DProjects.Utils {


    public class MailUtils {


        //inner classes
        public class Mail : IDisposable {
            public string From { get; set; } = "";
            public string[] To { get; set; } = [];
            public string[] CC { get; set; } = [];
            public string[] Bcc { get; set; } = [];
            public string[] ReplyTo { get; set; } = [];
            public string Subject { get; set; } = "";
            public string Body { get; set; } = "";
            public bool MessageIsHtml { get; set; }
            public HeadersUtils.Headers Headers { get; set; } = new();
            public List<Attachment> Attachments { get; set; } = [];

            public void Dispose() {
                Attachments.ForEach(x => x.Dispose());
            }
        }
        public class Attachment : IDisposable {
            public string Name { get; set; }
            public string ContentType { get; set; }
            public Stream Content { get; set; }
            public Attachment(byte[] buffer, string name, string mimetype) {
                Name = name;
                ContentType = mimetype;
                Content = new MemoryStream(buffer);
            }
            public void Dispose() {
                Content.Dispose();
            }
        }


        //inner classes
        public class MailPart {
            public HeadersUtils.Headers Headers { get; }
            public string Body { get; }
            public bool LastPart { get; }
            public MailPart[] ChildParts { get; }
            public MailPart(Stream stream, Encoding encoding, string? boundary = null) {
                Headers = HeadersUtils.ReadHttpHeaders(stream, encoding);
                Body = "";
                ChildParts = new MailPart[] { };
                var contentType = Headers.Get("Content-type", "");
                var contentCharset = StringUtils.GetConnectionStringVariable(contentType, "charset", "utf-8").Replace("\"", "");
                var contentTransferEncoding = Headers.Get("Content-Transfer-Encoding", "8bit"); //base64, quoted-printable, 8bit, 7bit, binary
                if (contentType.StartsWith("multipart")) {
                    var emailParts = new List<MailPart>();
                    var partBoundary = StringUtils.GetConnectionStringVariable(contentType, "boundary", "").Replace("\"", "");
                    while (true) {
                        var line = StreamUtils.ReadLine(stream, System.Text.Encoding.UTF7);
                        if (line == null) throw new Exception("Unable to decode eml file: syntax error");
                        if (line.StartsWith("--" + partBoundary)) break;
                    }
                    while (true) {
                        var emailPart = new MailPart(stream, encoding, partBoundary);
                        emailParts.Add(emailPart);
                        if (emailPart.LastPart) break;
                    }
                    ChildParts = emailParts.ToArray();
                } else {
                    var partEncoding = System.Text.Encoding.GetEncoding(contentCharset);
                    var body = new StringBuilder();
                    do {
                        var line = StreamUtils.ReadLine(stream, partEncoding);
                        if (line == null) break;
                        if (boundary != null && line.StartsWith("--" + boundary)) {
                            LastPart = line.EndsWith(boundary + "--");
                            break;
                        }
                        body.AppendLine(line);
                    } while (true);
                    Body = body.ToString();
                                  
                }
            }
            public string? GetPartAsHtml() {
                var contentType = Headers.Get("Content-type", "");
                if (contentType.StartsWith("text/html")) return DecodeAsText();
                foreach (var childPart in ChildParts) {
                    var html = childPart.GetPartAsHtml();
                    if (html != null) return html;
                }
                return null;
            }
            public string GetPartAsText() {
                var contentType = Headers.Get("Content-type", "");
                if (contentType.StartsWith("text/plain") || string.IsNullOrEmpty(contentType)) return DecodeAsText();
                foreach (var childPart in ChildParts) {
                    var text = childPart.GetPartAsText();
                    if (text != null) return text;
                }
                return "";
            }
            private string DecodeAsText() {
                var contentType = Headers.Get("Content-type", "");
                var contentEncoding = Encoding.GetEncoding(StringUtils.GetConnectionStringVariable(contentType, "charset", "utf-8").Replace("\"", ""));
                var contentTransferEncoding = Headers.Get("Content-Transfer-Encoding", "8bit"); //base64, quoted-printable, 8bit, 7bit, binary
                if (contentTransferEncoding == "base64") {
                    return contentEncoding.GetString(Base64Utils.FromBase64(Body.ToString()));
                } else if (contentTransferEncoding == "quoted-printable") {
                    return StringUtils.DecodeQuotedPrintable(Body.ToString(), contentEncoding);
                } else {
                    return Body.ToString();
                }
            }
            public Attachment[] GetAttachments() {
                var result = new List<Attachment>();
                var contentType = Headers.Get("Content-type", "");
                var contentCharset = StringUtils.GetConnectionStringVariable(contentType, "charset", "utf-8").Replace("\"", "");
                var contentTransferEncoding = Headers.Get("Content-Transfer-Encoding", "8bit"); //base64, quoted-printable, 8bit, 7bit, binary
                if (contentType.Length == 0) {
                } else if (contentType.StartsWith("text/")) {
                } else if (contentType.StartsWith("multipart/")) {
                } else {
                    var buffer = new byte[] { };
                    var partEncoding = System.Text.Encoding.GetEncoding(contentCharset);
                    if (contentTransferEncoding == "base64") {
                        buffer = Base64Utils.FromBase64(Body.ToString());
                    } else if (contentTransferEncoding == "quoted-printable") {
                        buffer = partEncoding.GetBytes(StringUtils.DecodeQuotedPrintable(Body.ToString(), partEncoding));
                    } else {
                        buffer = partEncoding.GetBytes(Body);
                    }
                    var filename = StringUtils.DecodeMimeEncodedString(StringUtils.GetConnectionStringVariable(contentType, "name", "").Replace("\"", ""));
                    var mimetype = MimeTypeUtils.GetMimeType(filename);
                    var mailAttachment = new Attachment(buffer, filename, mimetype);
                    result.Add(mailAttachment);
                }
                foreach (var childPart in ChildParts) {
                    result.AddRange(childPart.GetAttachments());
                }
                return result.ToArray(); ;
            }
            public Mail GetMail() {
                var mail = new Mail();
                //set mail headers
                mail.From = ParseHeaderEmailAddress(Headers.Get("From", ""));
                mail.To = ParseHeaderEmailAddresses(Headers.Get("To", ""));
                mail.CC = ParseHeaderEmailAddresses(Headers.Get("Cc", ""));
                mail.Bcc = ParseHeaderEmailAddresses(Headers.Get("Cco", ""));
                mail.Subject = StringUtils.DecodeMimeEncodedString(Headers.Get("Subject", ""));
                foreach (var item in Headers) {
                    if (String.Equals("from", item.Key, StringComparison.OrdinalIgnoreCase)) {
                    } else if (String.Equals("to", item.Key, StringComparison.OrdinalIgnoreCase)) {
                    } else if (String.Equals("cc", item.Key, StringComparison.OrdinalIgnoreCase)) {
                    } else if (String.Equals("cco", item.Key, StringComparison.OrdinalIgnoreCase)) {
                    } else if (String.Equals("subject", item.Key, StringComparison.OrdinalIgnoreCase)) {
                    } else if (item.Key.StartsWith("content-", StringComparison.OrdinalIgnoreCase)) {
                    } else {
                        mail.Headers.Set(item.Key, item.Value);
                    }
                }
                //set mail body
                var body = GetPartAsHtml();
                if (body != null) {
                    mail.Body = body;
                    mail.MessageIsHtml = true;
                } else {
                    mail.Body = GetPartAsText();
                    mail.MessageIsHtml = false;
                }
                //attachments
                mail.Attachments.AddRange(GetAttachments());
                //return
                return mail;
            }
        }


        //methods
        public static MailPart ParseEml(Stream stream, Encoding encoding) {
            return new MailPart(stream, encoding);           
        }
        public static Mail ParseEmlToMail(Stream stream, Encoding encoding) {
            return (new MailPart(stream, encoding)).GetMail();
        }
        public static Mail ParseEmlToMail(string text) {
            return (new MailPart(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)), System.Text.Encoding.UTF8)).GetMail();
        }
        public static string[] ParseHeaderEmailAddresses(string text) {
            var result = new List<string>();
            foreach (var part in text.Split(',')) {
                var address = ParseHeaderEmailAddress(part);
                if (!string.IsNullOrWhiteSpace(address)) result.Add(address);
            }
            return result.ToArray();
        }
        public static string ParseHeaderEmailAddress(string text) {
            if (text.IndexOf("<") != -1 ) {
                text = text.Substring(text.IndexOf("<")+1);
                if (text.IndexOf(">") != -1) {
                    return text.Substring(0, text.IndexOf(">"));
                }
            }
            return "";
        }
    }


}


