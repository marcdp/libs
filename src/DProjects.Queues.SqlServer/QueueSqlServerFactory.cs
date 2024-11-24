
using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Queues.SqlServer {

    [Protocol("sqlserver", "")]
    public class QueueSqlServerFactory : IFactoryByUrl<IQueue> {
        public IQueue Create(string src) {
            return new QueueSqlServer();
        }

    }

}
  