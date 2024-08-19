using DProjects.Utils;
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace DProjects.Text.Yaml.YamlConverters {


    public class ByteArrayBase64FoldedConverter : IYamlTypeConverter {

        //methods
        public bool Accepts(Type type) {
            return type == typeof(byte[]);
        }
        public object ReadYaml(IParser parser, Type type) {
            var scalar = (YamlDotNet.Core.Events.Scalar) parser.Current!;
            var value = scalar.Value as string;
            if (value.StartsWith("!!base64 ") || value.StartsWith("!!binary ")) value = value.Substring(value.IndexOf(" ") + 1);
            var bytes = Convert.FromBase64String(value);
            parser.MoveNext();
            return bytes;
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type) {
            var bytes = (byte[])value!;
            var aux = StringUtils.SplitByColumnsAndFold("!!base64 " + Convert.ToBase64String(bytes), 76);
            emitter.Emit(new YamlDotNet.Core.Events.Scalar(aux));
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) {
            throw new NotImplementedException();
        }
    }


}
