using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class DictionaryObjectObjectConverter : JsonConverter<Dictionary<object, object>> {

        //methods
        public override Dictionary<object, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }
        public override void Write(Utf8JsonWriter writer, Dictionary<object, object> value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach (var key in value.Keys) {
                writer.WritePropertyName(key.ToString());
                System.Text.Json.JsonSerializer.Serialize(writer, value[key], options);
            }
            writer.WriteEndObject();
        }

    }


}
