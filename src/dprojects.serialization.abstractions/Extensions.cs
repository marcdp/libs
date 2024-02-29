using System.IO;


namespace DProjects.Serialization {

    //interface
    public static class Extensions { 

        //methods
        public static string Serialize(this ISerializer serializer, object value) {
            return "";
        }
        public static object Deserialize<T>(this ISerializer serializer, string value) {
            return null;
        }

    }


}

