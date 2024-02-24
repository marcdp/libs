using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Log.Storage {

    [Protocol("null", "")]
    [ProtocolUsage("null:")]
    public class LogStorageNullFactory() : IFactoryByUrl<ILogStorage> {

        public ILogStorage Create(string url) {
            return new LogStorageNull();
        }

    }

}


