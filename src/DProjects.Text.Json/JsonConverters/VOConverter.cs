using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using DProjects.DataObjects;

namespace DProjects.Text.Json.JsonConverters {


    public class VOConverter : JsonConverter<VO> {


        //methods
        public override VO Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var jsonDocument = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            var vo = (VO)DeserializeVORecursive(jsonDocument, 0);
            return vo;
        }
        public override void Write(Utf8JsonWriter writer, VO value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            foreach (var a in value) {
                writer.WritePropertyName(a.Key);
                System.Text.Json.JsonSerializer.Serialize(writer, a.Value, options);
            }
            writer.WriteEndObject();
        }


        //private
        public static object DeserializeVORecursive(JsonElement jsonElement, int indent) {
            if (jsonElement.ValueKind == JsonValueKind.Object) {
                var result = new VO();
                foreach (var jsonProperty in jsonElement.EnumerateObject()) {
                    if (jsonProperty.Value.ValueKind == JsonValueKind.Array) {
                        result[jsonProperty.Name] = DeserializeVORecursive(jsonProperty.Value, indent + 1);
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.False) {
                        result[jsonProperty.Name] = false;
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.Null) {
                        result[jsonProperty.Name] = null;
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.Number) {
                        result[jsonProperty.Name] = jsonProperty.Value.GetDecimal();
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.Object) {
                        result[jsonProperty.Name] = DeserializeVORecursive(jsonProperty.Value, indent + 1);
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.String) {
                        result[jsonProperty.Name] = jsonProperty.Value.GetString();
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.True) {
                        result[jsonProperty.Name] = true;
                    } else if (jsonProperty.Value.ValueKind == JsonValueKind.Undefined) {
                    }
                }
                return result;
            } else if (jsonElement.ValueKind == JsonValueKind.Array) {
                var result = new List<object?>();
                foreach (var jsonArrayItem in jsonElement.EnumerateArray()) {
                    if (jsonArrayItem.ValueKind == JsonValueKind.Array) {
                        result.Add(DeserializeVORecursive(jsonArrayItem, indent + 1));
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.False) {
                        result.Add(true);
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.Null) {
                        result.Add(null);
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.Number) {
                        result.Add(jsonArrayItem.GetDecimal());
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.Object) {
                        result.Add(DeserializeVORecursive(jsonArrayItem, indent + 1));
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.String) {
                        result.Add(jsonArrayItem.GetString());
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.True) {
                        result.Add(true);
                    } else if (jsonArrayItem.ValueKind == JsonValueKind.Undefined) {
                    }
                }
                return result.ToArray();
            } else {
                throw new NotImplementedException("JsonDeserializer.DeserializeVORecursive: " + jsonElement.ValueKind);
            }
        }
    }


}
