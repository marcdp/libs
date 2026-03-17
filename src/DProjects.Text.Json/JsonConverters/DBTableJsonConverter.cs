//using DProjects.Db;
//using System;
//using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace DProjects.Text.Json.JsonConverters {


//    public class DBTableJsonConverter : JsonConverter<DBTable> {

//        //methods
//        public override DBTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
//            var result = new DBTable();
//            while (reader.Read()) {
//                var tokenType = reader.TokenType;
//                if (tokenType == JsonTokenType.EndObject) {
//                    break;
//                } else if (tokenType == JsonTokenType.PropertyName) {
//                    var propertyName = reader.GetString();
//                    if (propertyName == null) {
//                    } else if (propertyName.Equals("name")) {
//                        reader.Read();
//                        result.Name = reader.GetString() ?? "";
//                    } else if (propertyName.Equals("columns")) {
//                        var dbColumns = System.Text.Json.JsonSerializer.Deserialize<DBColumn[]>(ref reader, options);
//                        if (dbColumns!=null) {
//                            foreach (var dbColumn in dbColumns) {
//                                result.Columns.Add(dbColumn);
//                            }
//                        }
//                    } else if (propertyName.Equals("rows")) {
//                        var jsonElements = System.Text.Json.JsonSerializer.Deserialize<JsonElement[]>(ref reader, options);
//                        if (jsonElements != null) {
//                            foreach (var jsonElement in jsonElements) {
//                                var dict = (IDictionary<string, object?>)VOConverter.DeserializeVORecursive(jsonElement, 0);
//                                result.Rows.Add(dict);
//                            }
//                        }
//                    } else if (propertyName.Equals("extendedProperties")) {
//                        var jsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(ref reader, options);
//                        var vo = (VO)VOConverter.DeserializeVORecursive(jsonElement, 0);
//                        foreach (var key in vo.Keys) {
//                            result.ExtendedProperties.Add(key, vo[key]!);
//                        }
//                    }
//                }
//                if (tokenType == JsonTokenType.EndObject) break;
//            }
//            return result;
//        }
//        public override void Write(Utf8JsonWriter writer, DBTable value, JsonSerializerOptions options) {
//            writer.WriteStartObject();
//            writer.WriteString("name", value.Name);
//            //columns
//            writer.WriteStartArray("columns");
//            foreach (var dbColumn in value.Columns) {
//                System.Text.Json.JsonSerializer.Serialize(writer, dbColumn, options);
//            }
//            writer.WriteEndArray();
//            //rows
//            writer.WriteStartArray("rows");
//            foreach (var dbRow in value.Rows) {
//                writer.WriteStartObject();
//                foreach (var dbColumn in value.Columns) {
//                    writer.WritePropertyName(dbColumn.Name);
//                    System.Text.Json.JsonSerializer.Serialize(writer, dbRow[dbColumn.Name], options);
//                }
//                writer.WriteEndObject();
//            }
//            writer.WriteEndArray();
//            //extended properties
//            writer.WriteStartObject("extendedProperties");
//            foreach (var dbExtendedProperty in value.ExtendedProperties) {
//                writer.WritePropertyName(dbExtendedProperty.Key);
//                System.Text.Json.JsonSerializer.Serialize(writer, dbExtendedProperty.Value, options);
//            }
//            writer.WriteEndObject();
//            //end
//            writer.WriteEndObject();
//        }

//    }


//}
