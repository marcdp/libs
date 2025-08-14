using DProjects.Utils;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset> {

        private readonly string _format;

        public DateTimeOffsetConverter(string format) => _format = format;

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTimeOffset.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(_format));
    }


}
