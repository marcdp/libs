using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class NameValueCollectionConverter : JsonConverter<NameValueCollection> {

        //methods
        public override NameValueCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }
        public override void Write(Utf8JsonWriter writer, NameValueCollection value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach (string key in value.Keys) {
                writer.WritePropertyName(key.ToString());
                System.Text.Json.JsonSerializer.Serialize(writer, value[key], options);
            }
            writer.WriteEndObject();
        }

    }


}
