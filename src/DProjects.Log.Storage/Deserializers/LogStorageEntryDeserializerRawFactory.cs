
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Deserializers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    public class LogStorageEntryDeserializerRawFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

        public ILogStorageEntryDeserializer Create(string src) {
            return new LogStorageEntryDeserializerRaw();
        }
    }

}
