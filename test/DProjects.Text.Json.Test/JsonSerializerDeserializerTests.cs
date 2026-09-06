using Xunit;
using System.IO;
using System.Text;
using System.Xml;

namespace DProjects.Text.Json.Tests
{
    public class JsonSerializerDeserializerTests {

        //inner classes
        public record  Person {
            public int PropFirst { get; set; }
            public bool Prop2 { get; set; }
            public bool Prop3 { get; set; }
            public string? Prop4 { get; set; }
            public double Prop5 { get; set; }
            public DateTime Prop6 { get; set; }
            public bool Prop7 { get; set; }
            public string[] Prop8 { get; set; } = [];
            public int[] Prop9 { get; set; } = [];
            public bool[] Prop10 { get; set; } = [];
            public Address? Prop11 { get; set; }
            public Address[] Prop12 { get; set; } = [];
            public Kind Prop13 { get; set; } = Kind.None;
        }
        public record Address {
            public string Name { get; set; } = "";
            public int Number { get; set; }
        }
        public enum Kind {
            None,
            Kind1
        }

        //methods
        [Fact()]
        public void DefaultTest() {
            var person = new Person() {
                PropFirst = 123,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "hello",
                Prop5 = 123.23,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { "hello", "world" },
                Prop9 = new int[] { 0, 1, 2, 3 },
                Prop10 = new bool[] { true, false, true },
                Prop11 = new Address() { Name = "xxx", Number = 123 },
                Prop12 = [
                    new Address() { Name = "xxx", Number = 123 },
                    new Address() { Name = "xxxx", Number = 124 },
                ],
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new JsonSerializer(new() { });
            var json = serializer.Serialize(person); 
            Assert.Equal("""
                {"propFirst":123,"prop2":true,"prop3":true,"prop4":"hello","prop5":123.23,"prop6":"2020-01-01T01:01:01","prop7":false,"prop8":["hello","world"],"prop9":[0,1,2,3],"prop10":[true,false,true],"prop11":{"name":"xxx","number":123},"prop12":[{"name":"xxx","number":123},{"name":"xxxx","number":124}],"prop13":0}
                """, json);

            //deserialize
            var deserializer = new JsonDeserializer(new());
            var person2 = deserializer.Deserialize<Person>(json);
            Assert.Equal(serializer.Serialize(person), serializer.Serialize(person2));
        }
        [Fact()]
        public void SnakeCaseTest() {
            var person = new Person() {
                PropFirst = 123,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "hello",
                Prop5 = 123.23,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { "hello", "world" },
                Prop9 = new int[] { 0, 1, 2, 3 },
                Prop10 = new bool[] { true, false, true },
                Prop11 = new Address() { Name = "xxx", Number = 123 },
                Prop12 = [
                    new Address() { Name = "xxx", Number = 123 },
                    new Address() { Name = "xxxx", Number = 124 },
                ],
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new JsonSerializer(new() {
                NamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            });
            var json = serializer.Serialize(person);
            Assert.Equal("""
                {"prop_first":123,"prop2":true,"prop3":true,"prop4":"hello","prop5":123.23,"prop6":"2020-01-01T01:01:01","prop7":false,"prop8":["hello","world"],"prop9":[0,1,2,3],"prop10":[true,false,true],"prop11":{"name":"xxx","number":123},"prop12":[{"name":"xxx","number":123},{"name":"xxxx","number":124}],"prop13":0}
                """, json);

            //deserialize
            var deserializer = new JsonDeserializer(new() {
                NamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower
            });
            var person2 = deserializer.Deserialize<Person>(json);
            Assert.Equal(serializer.Serialize(person), serializer.Serialize(person2));
        }
        [Fact()]
        public void IgnoresTest() {
            var person = new Person() {
                PropFirst = 0,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "hello",
                Prop5 = 123.23,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { "hello", "world" },
                Prop9 = new int[] { 0, 1, 2, 3 },
                Prop10 = new bool[] { true, false, true },
                Prop11 = new Address() { Name = "xxx", Number = 123 },
                Prop12 = [
                    new Address() { Name = "xxx", Number = 123 },
                    new Address() { Name = "xxxx", Number = 124 },
                ],
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new JsonSerializer(new() {
                IgnoreDefaultValues = true,
                IgnoreNullValues = true, 
            });
            var json = serializer.Serialize(person);
            Assert.Equal("""
                {"prop2":true,"prop3":true,"prop4":"hello","prop5":123.23,"prop6":"2020-01-01T01:01:01","prop8":["hello","world"],"prop9":[0,1,2,3],"prop10":[true,false,true],"prop11":{"name":"xxx","number":123},"prop12":[{"name":"xxx","number":123},{"name":"xxxx","number":124}]}
                """, json);

            //deserialize
            var deserializer = new JsonDeserializer(new() {
            });
            var person2 = deserializer.Deserialize<Person>(json);
            Assert.Equal(serializer.Serialize(person), serializer.Serialize(person2));
        }
        [Fact()]
        public void WriteIndentedTest() {
            var person = new Person() {
                PropFirst = 0,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "hello",
                Prop5 = 123.23,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { "hello", "world" },
                Prop9 = new int[] { 0, 1, 2, 3 },
                Prop10 = new bool[] { true, false, true },
                Prop11 = new Address() { Name = "xxx", Number = 123 },
                Prop12 = [
                    new Address() { Name = "xxx", Number = 123 },
                    new Address() { Name = "xxxx", Number = 124 },
                ],
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new JsonSerializer(new() {
                WriteIndented = true
            });
            var json = serializer.Serialize(person);
            Assert.Equal(NormalizeLineEndings("""
                {
                  "propFirst": 0,
                  "prop2": true,
                  "prop3": true,
                  "prop4": "hello",
                  "prop5": 123.23,
                  "prop6": "2020-01-01T01:01:01",
                  "prop7": false,
                  "prop8": [
                    "hello",
                    "world"
                  ],
                  "prop9": [
                    0,
                    1,
                    2,
                    3
                  ],
                  "prop10": [
                    true,
                    false,
                    true
                  ],
                  "prop11": {
                    "name": "xxx",
                    "number": 123
                  },
                  "prop12": [
                    {
                      "name": "xxx",
                      "number": 123
                    },
                    {
                      "name": "xxxx",
                      "number": 124
                    }
                  ],
                  "prop13": 0
                }
                """), NormalizeLineEndings(json));

            //deserialize
            var deserializer = new JsonDeserializer(new() {
            });
            var person2 = deserializer.Deserialize<Person>(json);
            Assert.Equal(serializer.Serialize(person), serializer.Serialize(person2));
        }
        private static string NormalizeLineEndings(string value) {
            return value
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }
    }
}
