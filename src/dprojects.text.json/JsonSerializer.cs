using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DProjects.Text.Json {


    public class JsonSerializer(JsonSerializerSettings settings) : DProjects.Serialization.ISerializer {


        //methods
        public void Serialize(object value, Stream stream, Encoding encoding) {
            using var writer = new StreamWriter(stream, encoding, 1024, true);
            Serialize(value, writer);
        }
        public void Serialize(object? value, TextWriter writer) {
            var json = Serialize(value);
            writer.Write(json);
        }
        public async Task SerializeAsync(object? value, TextWriter writer) {
            var json = Serialize(value);
            await writer.WriteAsync(json);
        }
        public string Serialize(object? value) {
            if (value == null) return "null";
            var options = new JsonSerializerOptions();
            options.WriteIndented = settings.WriteIndented;
            options.IgnoreReadOnlyProperties = settings.IgnoreReadOnlyProperties;
            if (settings.IgnoreNullValues) options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNamingPolicy = settings.NamingPolicy;
            if (settings.IgnoreDefaultValues) options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault; 

            //options.Converters.Add(new JsonConverters.DBTableJsonConverter());
            options.Converters.Add(new JsonConverters.TypeJsonConverter());
            options.Converters.Add(new JsonConverters.DictionaryObjectObjectConverter());
            options.Converters.Add(new JsonConverters.NameValueCollectionConverter());
            options.Converters.Add(new JsonConverters.CultureInfoConverter());

            return System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), options);
        }

    }


}
