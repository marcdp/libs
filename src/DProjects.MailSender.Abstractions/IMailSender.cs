using System.Threading.Tasks;
using System.Threading;
using System.Net.Mail;

namespace DProjects.MailSender {

    public interface IMailSender {

        //methods
        Task SendAsync(MailMessage mail, CancellationToken cancellationToken);

    }
     


}