using System.IO;
using System.Text;


namespace DProjects.Serialization {

    //interface
    public interface IDeserializer { 

        //methods
        T Deserialize<T>(Stream stream, Encoding encoding);

    }


}

