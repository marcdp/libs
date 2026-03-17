using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;

namespace DProjects.Log {

    [Protocol("null", "")]
    [ProtocolUsage("null:")]
    public class LogNullFactory() : IFactoryByUrl<ILog> {

        public ILog Create(string url) {
            return new LogNull();
        }

    }

}


