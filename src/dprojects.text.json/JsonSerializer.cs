using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DProjects.Text.Json {


    public class JsonSerializer : DProjects.Serialization.ISerializer {

        //vars
        private JsonSerializerSettings mSettings;

        //ctor
        public JsonSerializer(JsonSerializerSettings? settings = null) {
            mSettings = settings ?? new JsonSerializerSettings();
        }

        //methods
        public void Serialize(object value, Stream stream, Encoding encoding) {
            using var writer = new StreamWriter(stream, encoding, 1024, true);
            Serialize(value, writer);
        }
        public void Serialize(object? value, TextWriter writer) {
            var json = this.Serialize(value);
            writer.Write(json);
        }
        public async Task SerializeAsync(object? value, TextWriter writer) {
            var json = this.Serialize(value);
            await writer.WriteAsync(json);
        }
        public string Serialize(object? value) {
            if (value == null) return "null";
            var options = new JsonSerializerOptions();
            options.WriteIndented = mSettings.WriteIndented;
            options.IgnoreReadOnlyProperties = mSettings.IgnoreReadOnlyProperties;
            if (mSettings.IgnoreNullValues) options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNamingPolicy = mSettings.NamingPolicy;
            if (mSettings.IgnoreDefaultValues) options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault; 
            //options.Converters.Add(new JsonConverters.DBTableJsonConverter());
            options.Converters.Add(new JsonConverters.TypeJsonConverter());
            options.Converters.Add(new JsonConverters.DictionaryObjectObjectConverter());
            options.Converters.Add(new JsonConverters.NameValueCollectionConverter());
            options.Converters.Add(new JsonConverters.CultureInfoConverter());

            return System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), options);
        }

    }


}
