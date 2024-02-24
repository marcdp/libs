
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log;
using DProjects.Log.Serializers;

namespace DProjects.Log.Serializers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    public class LogEntrySerializerRawFactory : IFactoryByUrl<ILogEntrySerializer> {

        public ILogEntrySerializer Create(string src) {
            return new LogEntrySerializerRaw();
        }
    }

}
