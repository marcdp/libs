using System;
using System.Collections.Generic;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DProjects.Text.Yaml.YamlConverters {


    public class DictionaryStringObjectConverter(string[] ignorePropertyNames): IYamlTypeConverter {

        //methods
        public bool Accepts(Type type) {
            return type == typeof(Dictionary<string,object?>);
        }
        public object ReadYaml(IParser parser, Type type) {
            throw new NotImplementedException();
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type) {
            if (value is Dictionary<string,object?> dictionary) {
                emitter.Emit(new MappingStart());
                foreach (var kvp in dictionary) {
                    if (System.Array.IndexOf(ignorePropertyNames, kvp.Key)!=-1) continue;
                    emitter.Emit(new Scalar(kvp.Key));
                    WriteValue(emitter, kvp.Value);
                }
                emitter.Emit(new MappingEnd());
            }
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) {
            throw new NotImplementedException();
        }

        private void WriteValue(IEmitter emitter, object? value) {
            if (value == null) {
                emitter.Emit(new Scalar("null"));
            } else if (value is string str) {
                emitter.Emit(new Scalar(str));
            } else if (value is IDictionary<string, object> dict) {
                WriteYaml(emitter, dict, dict.GetType());
            } else if (value is IEnumerable<object> list) {
                emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
                foreach (var item in list) {
                    WriteValue(emitter, item);
                }
                emitter.Emit(new SequenceEnd());
            } else {
                emitter.Emit(new Scalar(value.ToString()));
            }
        }

    }

}
