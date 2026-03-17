
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Deserializers {

    [Protocol("json", "")]
    [ProtocolExample("json:", "")]
    public class LogStorageEntryDeserializerJsonFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

        public ILogStorageEntryDeserializer Create(string src) {
            return new LogStorageEntryDeserializerJson();
        }
    }

}
