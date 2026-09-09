using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Db;

namespace DProjects.MailSender {

    public class MailSenderDb(IDBConnection dbConnection, string domain) : IMailSender {

        public async Task SendAsync(MailMessage mail, CancellationToken cancellationToken) {
            var emailUniqueId = Guid.NewGuid().ToString();
            var emailIndex = 0;
            var emailSource = "";
            foreach (var emailTo in mail.To) {
                await SendAsync(mail.From, emailTo.Address, mail.Subject, MaiMessageToEmlBuffer(mail), emailSource, emailUniqueId + "-" + (emailIndex++), cancellationToken);
            }
            foreach (var emailCC in mail.CC) {
                await SendAsync(mail.From, emailCC.Address, mail.Subject, MaiMessageToEmlBuffer(mail), emailSource, emailUniqueId + "-" + (emailIndex++), cancellationToken);
            }
            foreach (var emailBCC in mail.Bcc) {
                await SendAsync(mail.From, emailBCC.Address, mail.Subject, MaiMessageToEmlBuffer(mail), emailSource, emailUniqueId + "-" + (emailIndex++), cancellationToken);
            }
        }
        public async Task SendAsync(MailAddress emailFrom, string emailTo, string emailSubject, byte[] emailContent, string emailSource, string emailUniqueId, CancellationToken cancellationToken) {
            var recipients = new List<MailAddress>();
            var messageDomain = string.IsNullOrEmpty(domain) ? emailFrom.Address.Split('@')[1] : domain;
            var messageId = "<" + Guid.NewGuid().ToString() + "@" + messageDomain + ">";
            var sql = """
                INSERT INTO MailToSend ( 
                             emailFrom
                           , emailTo
                           , emailSubject
                           , emailMessageId
                           , emailContent
                           , emailSize
                           , emailSource
                           , emailUniqueid
                           , deliveryDate
                           , deliveryTries
                           , enqueuedDate
                           , emailContentLoaded
                           )
                     VALUES
                           (?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           ,?
                           )
                """;            
            await dbConnection.ExecuteNonQueryAsync(sql, [
                             emailFrom.Address
                           , emailTo
                           , emailSubject
                           , messageId
                           , emailContent
                           , emailContent.Length
                           , emailSource
                           , emailUniqueId
                           , null
                           , null
                           , DateTime.Now
                           , 1
                           ],
                           cancellationToken);
        }

        public byte[] MaiMessageToEmlBuffer(MailMessage mail) {
            var tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DProjects.MailSender-" + System.Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDirectory);
            try {
                using (var client = new SmtpClient(domain)) {
                    client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    client.PickupDirectoryLocation = tempDirectory;
                    client.Send(mail);
                }
                var filename = "";
                foreach(var aux in System.IO.Directory.GetFiles(tempDirectory)) {
                    filename = aux;
                }
                return RemovePrivateRecipientHeaders(System.IO.File.ReadAllBytes(filename));
            } finally {
                if (System.IO.Directory.Exists(tempDirectory)) System.IO.Directory.Delete(tempDirectory, true);
            }
        }


        // methods (private)
        private static byte[] RemovePrivateRecipientHeaders(byte[] eml) {
            // locate the header boundary without decoding or rewriting the MIME body
            var separator = FindHeaderSeparator(eml, out var separatorLength);
            if (separator < 0) return eml;
            var headerText = Encoding.ASCII.GetString(eml, 0, separator);
            var newline = headerText.Contains("\r\n") ? "\r\n" : "\n";
            var lines = headerText.Replace("\r\n", "\n").Split('\n');
            var retained = new List<string>();
            var removeContinuations = false;
            foreach (var line in lines) {
                if (line.StartsWith("Bcc:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("X-Receiver:", StringComparison.OrdinalIgnoreCase)) {
                    removeContinuations = true;
                    continue;
                }
                if (removeContinuations && (line.StartsWith(" ") || line.StartsWith("\t"))) continue;
                removeContinuations = false;
                retained.Add(line);
            }
            var sanitizedHeaders = Encoding.ASCII.GetBytes(string.Join(newline, retained));
            var result = new byte[sanitizedHeaders.Length + separatorLength + eml.Length - separator - separatorLength];
            Buffer.BlockCopy(sanitizedHeaders, 0, result, 0, sanitizedHeaders.Length);
            Buffer.BlockCopy(eml, separator, result, sanitizedHeaders.Length, eml.Length - separator);
            return result;
        }
        private static int FindHeaderSeparator(byte[] eml, out int separatorLength) {
            for (var i = 0; i < eml.Length - 1; i++) {
                if (i < eml.Length - 3 && eml[i] == '\r' && eml[i + 1] == '\n' && eml[i + 2] == '\r' && eml[i + 3] == '\n') {
                    separatorLength = 4;
                    return i;
                }
                if (eml[i] == '\n' && eml[i + 1] == '\n') {
                    separatorLength = 2;
                    return i;
                }
            }
            separatorLength = 0;
            return -1;
        }


    }

}
