
using System.Data;

using DProjects.Factories;
using DProjects.Db;
using DProjects.Factories.Attributes;


namespace DProjects.MailSender {

    [Protocol("db", "")]
    [ProtocolExample("db:", "")]
    public class MailSenderDbFactory(IFactoryByUrl<IDBConnection> dbConnectionFactory) : IFactoryByUrl<IMailSender> {
        public IMailSender Create(string src) {
            var url = new System.Uri(src);
            var dbConnection = dbConnectionFactory.Create(url.LocalPath);
            var domain = "";
            return new MailSenderDb(dbConnection, domain);
        }

    }

}
