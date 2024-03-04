
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Deserializers {

    [Protocol("auto", "")]
    [ProtocolExample("auto:", "")]
    public class LogStorageEntryDeserializerAutoFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

        public ILogStorageEntryDeserializer Create(string src) {
            return new LogStorageEntryDeserializerAuto();
        }
    }

}
