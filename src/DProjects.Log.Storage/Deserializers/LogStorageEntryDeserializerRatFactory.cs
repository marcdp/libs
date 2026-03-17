
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Deserializers {

    [Protocol("rat", "")]
    [ProtocolExample("rat:", "")]
    public class LogStorageEntryDeserializerRatFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

        public ILogStorageEntryDeserializer Create(string src) {
            return new LogStorageEntryDeserializerRat();
        }
    }

}
