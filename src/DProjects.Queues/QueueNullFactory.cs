
using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Queues {

    [Protocol("null", "")]
    public class QueueNullFactory : IFactoryByUrl<IQueue> {
        public IQueue Create(string src) {
            return new QueueNull();
        }

    }

}
  