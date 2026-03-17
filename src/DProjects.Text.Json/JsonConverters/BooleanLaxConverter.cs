using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class BooleanLaxConverter : JsonConverter<bool> {

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) return false;
            if (reader.TokenType == JsonTokenType.False) return false;
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.String) {
                var value = reader.GetString();
                if (value == null) return false;
                if (value.Equals("true") || value.Equals("yes") || value.Equals("1")) {
                    return true;
                }
                if (value.ToLower().Equals("false") || value.Equals("no") || value.Equals("0")) {
                    return false;
                }
            }
            return false;
        }
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) {
            writer.WriteStringValue(value ? "true" : "false");
        }

    }


}
