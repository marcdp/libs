using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DProjects.Text.Json {


    public class JsonDeserializer(JsonDeserializerSettings settings) : DProjects.Serialization.IDeserializer  {

        //methods

        public T Deserialize<T>(Stream stream, Encoding encoding) {
            return (T)Deserialize<T>(new StreamReader(stream, encoding))!;
        }
        public T Deserialize<T>(TextReader textReader) {
            return (T)Deserialize<T>(textReader.ReadToEnd())!;
        }
        public T Deserialize<T>(string json) {
            return (T)Deserialize(json, typeof(T))!;
        }
        public object? Deserialize(string json, Type returnType) {
            var options = new JsonSerializerOptions();
            //options.Converters.Add(new JsonConverters.DBTableJsonConverter());
            options.Converters.Add(new JsonConverters.TypeJsonConverter());
            //options.Converters.Add(new JsonConverters.VOConverter());
            options.Converters.Add(new JsonConverters.CultureInfoConverter());
            options.PropertyNameCaseInsensitive = settings.PropertyNameCaseInsensitive;
            if (settings.UseIntLaxConverter) options.Converters.Add(new JsonConverters.IntLaxConverter());
            if (settings.UseBooleanLaxConverter) options.Converters.Add(new JsonConverters.BooleanLaxConverter());
            if (settings.UseDateTimeLaxConverter) options.Converters.Add(new JsonConverters.DateTimeLaxConverter());
            options.AllowTrailingCommas = settings.AllowTrailingCommas;
            options.IncludeFields = settings.IncludeFields;
            options.PropertyNamingPolicy = settings.NamingPolicy;
            if (returnType == typeof(JsonDocument)) {
                return JsonDocument.Parse(json);
            //} else if (returnType == typeof(object[])) {
            //    var vo = System.Text.Json.JsonSerializer.Deserialize<VO>("{\"array\":" + json + "}", options); ;
            //    if (vo == null) return null;
            //    var result = vo.Get<object[]>("array");
            //    return result;
            } else if (returnType == typeof(IDictionary<string, object?>)) {
                var jsonDocument = System.Text.Json.JsonSerializer.Deserialize<JsonDocument>(json, options); ;
                if (jsonDocument == null) return null;
                return (IDictionary<string, object?>) JsonElementToObject(jsonDocument.RootElement)!;
            } else {
                return System.Text.Json.JsonSerializer.Deserialize(json, returnType, options); ;
            }
        }

        //private methods
        private object? JsonElementToObject(JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.Array) {
                var result = new List<object?>();
                foreach (var node in jsonElement.EnumerateArray()) {
                    result.Add(JsonElementToObject(node));
                }
                return result;
            } else if (jsonElement.ValueKind == JsonValueKind.False) {
                return false;
            } else if (jsonElement.ValueKind == JsonValueKind.Null) {
                return null;
            } else if (jsonElement.ValueKind == JsonValueKind.Number) {
                if (jsonElement.TryGetInt32(out int valueInt32)) return valueInt32;
                if (jsonElement.TryGetInt64(out long valueInt64)) return valueInt64;
                if (jsonElement.TryGetSingle(out float valueFloat)) return valueFloat;
                if (jsonElement.TryGetDouble(out double valueDouble)) return valueDouble;
                if (jsonElement.TryGetDecimal(out decimal valueDecimal)) return valueDecimal;
                throw new Exception("Unable to deserialize json numeric value: " + jsonElement.GetRawText());
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                var result = new Dictionary<string, object?>();
                foreach (var node in jsonElement.EnumerateObject()) {
                    result[node.Name] = JsonElementToObject(node.Value);
                }
                return result;
            } else if (jsonElement.ValueKind == JsonValueKind.String) {
                var value = jsonElement.GetString();
                if (value == null) {
                    return null;
                } else if (settings.UseBooleanLaxConverter && (value.Equals("true") || value.Equals("yes") || value.Equals("1"))) {
                    return true;
                } else if (settings.UseBooleanLaxConverter && (value.Equals("false") || value.Equals("no") || value.Equals("0"))) {
                    return false;
                } else if (settings.UseDateTimeLaxConverter && DateTimeUtils.TryParse(value, out DateTime valueDateTime)) {
                    return valueDateTime;
                } else {
                    return value;
                }
            } else if (jsonElement.ValueKind == JsonValueKind.True) {
                return true;
            } else if (jsonElement.ValueKind == JsonValueKind.Undefined) {
                return null;
            } else {
                return jsonElement.GetString();
            }
        }
    }


}
