using Xunit;
using System.IO;
using System.Text;
using System.Xml;
using DProjects.Text.Xml;

namespace DProjects.Text.Xml.Tests
{
    public class XmlSerializerDeserializerTests {

        //inner classes
        public record  Person {
            public int Prop1 { get; set; }
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
        public void AliasTest() {
            var person = new Person() {
                Prop1 = 123,
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
            var serializer = new XmlSerializer(new() { 
                 Alias = new() { 
                     { "prop2", "PPPPPPP22222" } 
                 } ,
                 
                  
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);

            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <person prop1="123" PPPPPPP22222="true" prop3="true" prop4="hello" prop5="123.2" prop6="2020-01-01T01:01:01" prop8="hello,world" prop9="0,1,2,3" prop10="true,false,true">
                  <prop11 name="xxx" number="123" />
                  <prop12>
                    <address name="xxx" number="123" />
                    <address name="xxxx" number="124" />
                  </prop12>
                </person>
                """), NormalizeLineEndings(xml));
        }


        [Fact()]
        public void EnumTest() {
            var person = new Person() {
                Prop1 = 123,
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
                Prop13 = Kind.Kind1
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new XmlSerializer(new() {
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);

            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <person prop1="123" prop2="true" prop3="true" prop4="hello" prop5="123.2" prop6="2020-01-01T01:01:01" prop8="hello,world" prop9="0,1,2,3" prop10="true,false,true" prop13="kind1">
                  <prop11 name="xxx" number="123" />
                  <prop12>
                    <address name="xxx" number="123" />
                    <address name="xxxx" number="124" />
                  </prop12>
                </person>
                """), NormalizeLineEndings(xml));

            //deserialize
            var deserialize = new XmlDeserializer(new() {
            }); ;
            var person2 = deserialize.Deserialize<Person>(xml);
            Assert.Equal(xml, serializer.SerializeToStringUTF8NoBom(person2));
        }


        [Fact()]
        public void NamingModeTest() {
            var person = new Person() {
                Prop1 = 123,
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
            //serialize
            var serializer = new XmlSerializer(new() {
                NamingMode = XmlSerializerSettings.NamingModes.None 
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);
            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <Person Prop1="123" Prop2="true" Prop3="true" Prop4="hello" Prop5="123.2" Prop6="2020-01-01T01:01:01" Prop8="hello,world" Prop9="0,1,2,3" Prop10="true,false,true">
                  <Prop11 Name="xxx" Number="123" />
                  <Prop12>
                    <Address Name="xxx" Number="123" />
                    <Address Name="xxxx" Number="124" />
                  </Prop12>
                </Person>
                """), NormalizeLineEndings(xml));

            //deserialize
            var deserialize = new XmlDeserializer(new() {
                 NamingMode = XmlDeserializerSettings.NamingModes.None
            }); ;
            var person2 = deserialize.Deserialize<Person>(xml);
            Assert.Equal(xml, serializer.SerializeToStringUTF8NoBom(person2));
        }

        [Fact()]
        public void UnPrefixTest() {
            var person = new Person() {
                Prop1 = 123,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "hello",
                Prop5 = 123.2,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { "hello", "world" },
                Prop9 = new int[] { 1, 2, 3 },
                Prop10 = new bool[] { true, false, true },
                Prop11 = new Address() { Name = "xxx", Number = 123 },
                Prop12 = [
                    new Address() { Name = "xxx", Number = 123 },
                    new Address() { Name = "xxxx", Number = 124 },
                ],
            };
            //serialize
            var serializer = new XmlSerializer(new() {
                Unprefixes = ["Pe","Ad"]
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);
            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <rson prop1="123" prop2="true" prop3="true" prop4="hello" prop5="123.2" prop6="2020-01-01T01:01:01" prop8="hello,world" prop9="1,2,3" prop10="true,false,true">
                  <prop11 name="xxx" number="123" />
                  <prop12>
                    <dress name="xxx" number="123" />
                    <dress name="xxxx" number="124" />
                  </prop12>
                </rson>
                """), NormalizeLineEndings(xml));


            //deserialize
            var deserialize = new XmlDeserializer(new() {
                TypePrefix = "Pe"
            }); ;
            var person2 = deserialize.Deserialize<Person>(xml);
            Assert.Equal(xml, serializer.SerializeToStringUTF8NoBom(person2));

        }


        [Fact()]
        public void AvoidsTrueTest() {
            var person = new Person() {
                Prop1 = 123,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "",
                Prop5 = 0,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] {},
                Prop9 = new int[] { },
                Prop10 = new bool[] { },
                Prop11 = new Address() { Name = "", Number = 123 },
                Prop12 = [],
                Prop13 = Kind.None
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new XmlSerializer(new() {
                 AvoidEmptyArrays = true,
                 AvoidEmptyStrings = true,
                 AvoidDefaultEnumValues = true,
                 AvoidFalseBooleans = true,
                 AvoidZeroNumbers = true
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);

            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <person prop1="123" prop2="true" prop3="true" prop6="2020-01-01T01:01:01">
                  <prop11 number="123" />
                </person>
                """), NormalizeLineEndings(xml));

            //deserialize
            var deserialize = new XmlDeserializer(new() {
            }); ;
            var person2 = deserialize.Deserialize<Person>(xml);
            Assert.Equal(xml, serializer.SerializeToStringUTF8NoBom(person2));
        }
        [Fact()]
        public void AvoidsFalseTest() {
            var person = new Person() {
                Prop1 = 123,
                Prop2 = true,
                Prop3 = true,
                Prop4 = "",
                Prop5 = 0,
                Prop6 = new DateTime(2020, 1, 1, 1, 1, 1),
                Prop7 = false,
                Prop8 = new string[] { },
                Prop9 = new int[] { },
                Prop10 = new bool[] { },
                Prop11 = new Address() { Name = "", Number = 123 },
                Prop12 = [],
                Prop13 = Kind.None
            };
            //var settings = new XmlSerializerSettings();
            var serializer = new XmlSerializer(new() {
                AvoidEmptyArrays = false,
                AvoidEmptyStrings = false,
                AvoidDefaultEnumValues = false,
                AvoidFalseBooleans = false,
                AvoidZeroNumbers = false
            });
            var xml = serializer.SerializeToStringUTF8NoBom(person);

            Assert.Equal(NormalizeLineEndings("""
                <?xml version="1.0" encoding="utf-8"?>
                <person prop1="123" prop2="true" prop3="true" prop4="" prop5="0.0" prop6="2020-01-01T01:01:01" prop7="false" prop8="" prop9="" prop10="" prop13="none">
                  <prop11 name="" number="123" />
                  <prop12 />
                </person>
                """), NormalizeLineEndings(xml));

            //deserialize
            var deserialize = new XmlDeserializer(new() {
            }); ;
            var person2 = deserialize.Deserialize<Person>(xml);
            Assert.Equal(xml, serializer.SerializeToStringUTF8NoBom(person2));
        }

        private static string NormalizeLineEndings(string value) {
            return value
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }
    }
}
