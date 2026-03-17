using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DProjects.DataTypes;
using System.Globalization;

namespace DProjects.Text.Json.JsonConverters {


    public class Double6DigitsConverter : JsonConverter<double> {


        //methods
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) {
            // to avoid scientific notation, use custom format
            var s = value.ToString("0.######", CultureInfo.InvariantCulture);
            writer.WriteRawValue(s);
        }
    }


}
