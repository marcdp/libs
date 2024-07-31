
using DProjects.Factories;
using DProjects.Factories.Attributes;


namespace DProjects.MailSender {

    [Protocol("null", "")]
    [ProtocolExample("null:", "")]
    public class MailSenderNullFactory() : IFactoryByUrl<IMailSender> {
        public IMailSender Create(string src) {
            var url = new System.Uri(src);
            return new MailSenderNull();
        }

    }

}
