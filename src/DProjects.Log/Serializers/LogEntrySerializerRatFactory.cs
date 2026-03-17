
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Log;
using DProjects.Log.Serializers;

namespace DProjects.Log.Serializers {

    [Protocol("rat", "")]
    [ProtocolExample("rat:", "")]
    public class LogEntrySerializerRatFactory : IFactoryByUrl<ILogEntrySerializer> {

        public ILogEntrySerializer Create(string src) {
            return new LogEntrySerializerRat();
        }
    }

}
