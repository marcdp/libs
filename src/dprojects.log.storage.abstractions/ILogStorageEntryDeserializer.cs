
namespace DProjects.Log.Storage {


    //interface
    public interface ILogStorageEntryDeserializer {

        //methods
        LogEntry Deserialize(string line);

    }


}

