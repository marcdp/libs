using DProjects.DataTypes;
using DProjects.Utils;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class TimestampConverter : JsonConverter<Timestamp> {

        public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            long unixMs = reader.GetInt64();
            return new Timestamp(unixMs);
        }

        public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options) {
            writer.WriteNumberValue(value.UnixMs);
        }

    }


}
