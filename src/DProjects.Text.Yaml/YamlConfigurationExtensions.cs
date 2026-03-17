using DProjects.Utils;
using System.Collections.Specialized;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DProjects.Text.Yaml {


    public static class YamlConfigurationExtensions {

        public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path) {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yamlObject = deserializer.Deserialize<object>(yaml);
            var serializer = new SerializerBuilder().JsonCompatible().Build();

            // Convert YAML -> JSON string -> memory stream
            var json = serializer.Serialize(yamlObject);  
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            builder.AddJsonStream(stream);
            return builder;
        }
    }


}




