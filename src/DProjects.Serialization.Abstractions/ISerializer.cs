using System.IO;
using System.Text;


namespace DProjects.Serialization {

    //interface
    public interface ISerializer { 

        //methods
        void Serialize(object value, Stream stream, Encoding encoding);   

    }


}

