using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DProjects.Text.Yaml.Tests {

    public class YamlSerializerTests {
         
        [Fact]
        public void Serialize_WithNullObject_ThrowsArgumentNullException() {
            //arrange
            var serializer = new YamlSerializer(new());
            var expected = "~";
            //act
            var actual = serializer.Serialize(null);
            //assert
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void Serialize_WithObject_ReturnsYamlString() {
            //arrange
            var serializer = new YamlSerializer(new());
            var obj = new {
                Name = "John",
                Age = 30
            };
            var expected = "name: John\r\nage: 30\r\n";
            //act
            var actual = serializer.Serialize(obj);
            //assert
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void Serialize_WithObject_ReturnsYamlStringWithNestedObjects() {
            //arrange
            var serializer = new YamlSerializer(new());
            var obj = new {
                Name = "John",
                Age = 30,
                Address = new {
                    Street = "123 Main St",
                    City = "Anytown"
                }
            };
            var expected = "name: John\r\nage: 30\r\naddress:\r\n  street: 123 Main St\r\n  city: Anytown\r\n";
            //act
            var actual = serializer.Serialize(obj);
            //assert
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void Serialize_WithObject_ReturnsYamlStringWithArray() {
            //arrange
            var serializer = new YamlSerializer(new());
            var obj = new {
                Name = "John",
                Age = 30,
                Children = new[] { "Alice", "Bob", "Charlie" }
            };
            var expected = "name: John\r\nage: 30\r\nchildren:\r\n- Alice\r\n- Bob\r\n- Charlie\r\n";
            //act
            var actual = serializer.Serialize(obj);
            //assert
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void Serialize_WithObject_ReturnsYamlStringWithArrayAndFrontMatter() {
            //arrange
            var serializer = new YamlSerializer(new() { 
                FrontMatter = true,
                IgnorePropertyNames = new[] { "address" },
            });
            var obj = new {
                Name = "John",
                Age = 30,
                Children = new[] { "Alice", "Bob", "Charlie" },
                Address = new {
                    Street = "123 Main St",
                    City = "Anytown"
                },
                Bytes = new byte[] { 1,2,3,4,5,6,7,8,9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9}
            };
            var expected = "---\r\nname: John\r\nage: 30\r\nchildren:\r\n- Alice\r\n- Bob\r\n- Charlie\r\nbytes: !!binary AQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJ\r\n---\r\n";
            //act
            var actual = serializer.Serialize(obj);
            //assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Serialize_WithObject_ReturnsYamlStringWithArrayAndFrontMatterandBinaryBase64() {
            //arrange
            var serializer = new YamlSerializer(new() {
                FrontMatter = true,
                IgnorePropertyNames = new[] { "Address" },
                BinaryMode = YamlSerializerSettings.BinaryModes.Base64Folded,
                NamingMode = YamlSerializerSettings.NamingModes.None,
            });
            var obj = new {
                Name = "John",
                Age = 30,
                Children = new[] { "Alice", "Bob", "Charlie" },
                Address = new {
                    Street = "123 Main St",
                    City = "Anytown" 
                },
                Bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };
            var expected = "---\r\nName: John\r\nAge: 30\r\nChildren:\r\n- Alice\r\n- Bob\r\n- Charlie\r\nBytes: >-\r\n  !!base64 AQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAUGBwgJAQIDBAU\r\n\r\n  GBwgJAQIDBAUGBwgJ\r\n---\r\n";
            //act
            var actual = serializer.Serialize(obj);
            //assert
            Assert.Equal(expected, actual);
        }
    }
}