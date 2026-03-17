using DProjects.Utils;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DProjects.Text.Json.JsonConverters {


    public class TypeJsonConverter : JsonConverter<Type> {

        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException(); //per motius de seguretat, ho deshabilitem
            //var name = reader.GetString();
            //var type = ConvertUtils.To<Type>(name);
            //return type;
        }
        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) {
            var name = ConvertUtils.To<string>(value);
            writer.WriteStringValue(name);
        }

    }


}
