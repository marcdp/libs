
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Deserializers {

    [Protocol("classic", "")]
    [ProtocolExample("classic:", "")]
    public class LogStorageEntryDeserializerClassicFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

        public ILogStorageEntryDeserializer Create(string src) {
            return new LogStorageEntryDeserializerClassic();
        }
    }

}
