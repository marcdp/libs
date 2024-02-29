using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class CultureInfoConverter : JsonConverter<CultureInfo> {

        public override CultureInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var name = reader.GetString();
            if (name == null) return null;
            var type = new CultureInfo(name);
            return type;
        }
        public override void Write(Utf8JsonWriter writer, CultureInfo cultureInfo, JsonSerializerOptions options) {
            writer.WriteStringValue(cultureInfo.Name);
        }

    }


}
