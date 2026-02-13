using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DProjects.DataTypes;
using System.Globalization;

namespace DProjects.Text.Json.JsonConverters {


    public class Decimal6DigitsConverter : JsonConverter<decimal> {

        //methods
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return reader.GetDecimal();
        }
        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) {
            // redondea a 6 decimales sin cambiar el valor original en memoria
            var rounded = Math.Round(value, 6, MidpointRounding.AwayFromZero);
            writer.WriteNumberValue(rounded);
        }
    }


}
