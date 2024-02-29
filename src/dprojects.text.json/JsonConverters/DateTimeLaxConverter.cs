using DProjects.Utils;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class DateTimeLaxConverter : JsonConverter<DateTime> {

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) return default;
            if (reader.TokenType == JsonTokenType.String) {
                var value = reader.GetString();
                if (value == null) {
                    return default;
                } else if (DateTimeUtils.TryParse(value, out DateTime result)) {
                    return result;
                } else {
                    throw new Exception("Unable to deserialize DateTime: unable to parse: " + value);
                }
            }
            throw new Exception("Unable to deserialize DateTime: invalid token type: " + reader.TokenType.ToString());
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
            if (value == default) {
                writer.WriteNullValue();
            } else {
                writer.WriteStringValue(value.ToString(DateTimeUtils.DATETIME_ISO8601));
            }
        }

    }


}
