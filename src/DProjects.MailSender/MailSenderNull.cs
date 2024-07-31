using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.MailSender {

    public class MailSenderNull : IMailSender {

        public Task SendAsync(MailMessage mail, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


    }

}