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
            public string Name { get; set; }
            public int Age { get; set; }
            public string[] Keys { get; set; }
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

    }
}