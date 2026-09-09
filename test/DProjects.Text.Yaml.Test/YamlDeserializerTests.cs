using Xunit;
using DProjects.Utils;
using DProjects.Text.Yaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DProjects.Text.Yaml.Tests {

    public class YamlDeserializerTests {
         
        //inner class
        public class Person {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public string[] Keys { get; set; } = [];
        }

        [Fact]
        public void DeserializeTest() {
            //Arrange
            var yaml = "---\nname: \"John\"\nage: 30\n---\n";
            var deserializer = new YamlDeserializer(new() { 
                 ExpectFrontMatter = true,
            });
            var expected = new Person() {
                Name = "John",
                Age = 30
            };
            //Act
            var actual = deserializer.Deserialize<Person>(yaml);
            //Assert
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Age, actual.Age);
        }
        [Fact]
        public void DeserializeTest2() {
            //Arrange
            var yaml = "Name: \"John\"\nAge: 30\nKeys:\n- first\n- second\n";
            var deserializer = new YamlDeserializer(new() {
                ExpectFrontMatter = false,
                NamingMode = YamlDeserializerSettings.NamingModes.None
            });
            var expected = new Person() {
                Name = "John",
                Age = 30,
                Keys = ["first", "second"]
            };
            //Act
            var actual = deserializer.Deserialize<Person>(yaml);
            //Assert
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Age, actual.Age);
            Assert.Equal(expected.Keys, actual.Keys);
        }
        [Fact]
        public void SerializerDeserializer_RoundTripsCollectionsNullAndBinary() {
            var expected = new Payload { Name = "payload", Optional = null, Values = new Dictionary<string, int> { ["one"] = 1 }, Bytes = [0, 1, 2, 255] };
            var yaml = new YamlSerializer(new()).Serialize(expected);

            var actual = new YamlDeserializer(new()).Deserialize<Payload>(yaml);

            Assert.Equal(expected.Name, actual.Name);
            Assert.Null(actual.Optional);
            Assert.Equal(expected.Values, actual.Values);
            Assert.Equal(expected.Bytes, actual.Bytes);
        }
        [Fact]
        public void MalformedYaml_IsRejected() {
            var deserializer = new YamlDeserializer(new());

            Assert.ThrowsAny<Exception>(() => deserializer.Deserialize<Person>("name: [unterminated"));
        }
        [Fact]
        public void MalformedFrontMatter_IsRejected() {
            var deserializer = new YamlDeserializer(new() { ExpectFrontMatter = true });

            Assert.ThrowsAny<Exception>(() => deserializer.Deserialize<Person>("---\nname: [unterminated\n---"));
        }

        public class Payload {

            // props
            public string Name { get; set; } = "";
            public string? Optional { get; set; }
            public Dictionary<string, int> Values { get; set; } = [];
            public byte[] Bytes { get; set; } = [];
        }

    }
}
