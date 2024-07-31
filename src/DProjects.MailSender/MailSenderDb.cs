using System;
using System.Collections.Generic;
using System.Net.Mail;
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
            if (string.IsNullOrEmpty(domain)) domain = emailFrom.Address.Split('@')[1];
            var messageId = "<" + Guid.NewGuid().ToString() + "@" + domain + ">";
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
            SmtpClient client = new SmtpClient(domain);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            var tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDirectory);
            client.PickupDirectoryLocation = tempDirectory;
            client.Send(mail);
            var filename = "";
            foreach(var aux in System.IO.Directory.GetFiles(tempDirectory)) {
                filename = aux;
            }
            var result = System.IO.File.ReadAllBytes(filename);
            System.IO.Directory.Delete (tempDirectory, true);
            return result;
        }


    }

}