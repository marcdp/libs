
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log;
using DProjects.Log.Serializers;

namespace DProjects.Log.Serializers {

    [Protocol("json", "")]
    [ProtocolExample("json:", "")]
    public class LogEntrySerializerJsonFactory : IFactoryByUrl<ILogEntrySerializer> {

        public ILogEntrySerializer Create(string src) {
            return new LogEntrySerializerJson();
        }
    }

}
