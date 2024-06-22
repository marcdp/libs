using System.IO;


namespace DProjects.Serialization {

    //interface
    public static class Extensions { 

        //methods
        public static string Serialize(this ISerializer serializer, object value) {
            using var ms = new MemoryStream();
            serializer.Serialize(value, ms, System.Text.Encoding.UTF8);
            var buffer = ms.ToArray();
            return System.Text.Encoding.UTF8.GetString(buffer);
        }
        public static object? Deserialize<T>(this IDeserializer deSerializer, string value) {
            return deSerializer.Deserialize<T>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)), System.Text.Encoding.UTF8);
        }

    }


}

