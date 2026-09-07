using Xunit;
using System.IO;
using System.Text;
using DProjects.Log;
using DProjects.Utils;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Tests {

    public class LogDeserializersTests : Base {

        //tests
        [Theory()]
        [InlineData("json:", """
            {"timestamp":"2020-01-01T00:00:00.0000000Z","level":"Information","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"fields":{"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}}
            {"timestamp":"2020-01-01T00:00:00.0000000Z","level":"Warning","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"fields":{"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}}
            {"timestamp":"2020-01-01T00:00:00.0000000Z","level":"Error","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"fields":{"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}}
            {"timestamp":"2020-01-01T00:00:00.0000000Z","level":"Fatal","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"fields":{"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}}
            """)]
        [InlineData("rat:", """
            2020-01-01T00:00:00.000Z [info|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [warn|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [error|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [fatal|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            """)]
        [InlineData("raw:", """
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello
            """)]
        public void WriterTest(string protocol, string log) {
            var sw = new StringWriter();
            var serializer = mLogEntrySerializerFactoryByUrl.Create(protocol);
            var deserializer = mLogStorageEntryDeserializerFactoryByUrl.Create(protocol);
            foreach(var line in log.Replace("" + CharUtils.CHAR_CR, "").Split(CharUtils.CHAR_LF)) {
                var logEntry = deserializer.Deserialize(line);
                var line2 = serializer.Serialize(logEntry);
                Assert.Equal(line, line2);
            }
        }
        [Theory]
        [InlineData("Trace", LogLevel.Trace)]
        [InlineData("Debug", LogLevel.Debug)]
        [InlineData("Information", LogLevel.Information)]
        [InlineData("Warning", LogLevel.Warning)]
        [InlineData("Error", LogLevel.Error)]
        [InlineData("Critical", LogLevel.Fatal)]
        [InlineData("Fatal", LogLevel.Fatal)]
        [InlineData("Severe", LogLevel.Fatal)]
        public void Auto_RecognizesEveryClassicSeverity(string severity, LogLevel expectedLevel) {
            var deserializer = new LogStorageEntryDeserializerAuto();

            var entry = deserializer.Deserialize(severity + "|2018-04-26 00:00:00 34|source|message|0||user|");

            Assert.Equal(expectedLevel, entry.Level);
            Assert.Equal("message", entry.Message);
            Assert.NotEqual(DateTime.MinValue, entry.Date);
        }
        [Fact]
        public void Classic_RejectsUnknownSeverity() {
            var deserializer = new LogStorageEntryDeserializerClassic();

            var exception = Assert.Throws<FormatException>(() => deserializer.Deserialize("Verbose|2018-04-26 00:00:00 34|source|message|0||user|"));

            Assert.IsType<FormatException>(exception.InnerException);
        }
        [Fact]
        public void Auto_RejectsUnknownSeverityInClassicShapedRecord() {
            var deserializer = new LogStorageEntryDeserializerAuto();

            Assert.Throws<FormatException>(() => deserializer.Deserialize("Verbose|2018-04-26 00:00:00 34|source|message|0||user|"));
        }
        [Fact]
        public void Auto_KeepsNonClassicRecordContainingPipesAsRaw() {
            const string record = "raw message | with | pipes";
            var deserializer = new LogStorageEntryDeserializerAuto();

            var entry = deserializer.Deserialize(record);

            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal(DateTime.MinValue, entry.Date);
            Assert.Equal(record, entry.Message);
        }
        [Theory]
        [InlineData("classic", "Secret|credential-value|source|message|", "Unable to parse Classic log record.")]
        [InlineData("rat", "credential-value", "Unable to parse RAT log record.")]
        [InlineData("json", "{credential-value", "Unable to parse JSON log record.")]
        public void MalformedRecord_DoesNotExposeRawContent(string format, string record, string expectedMessage) {
            ILogStorageEntryDeserializer deserializer = format switch {
                "classic" => new LogStorageEntryDeserializerClassic(),
                "rat" => new LogStorageEntryDeserializerRat(),
                "json" => new LogStorageEntryDeserializerJson(),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            var exception = Assert.Throws<FormatException>(() => deserializer.Deserialize(record));

            Assert.Equal(expectedMessage, exception.Message);
            Assert.DoesNotContain("credential-value", exception.Message);
            Assert.NotNull(exception.InnerException);
        }

    }
}
