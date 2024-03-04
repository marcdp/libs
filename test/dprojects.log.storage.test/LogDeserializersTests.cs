using Xunit;
using System.IO;
using System.Text;
using DProjects.Log;
using DProjects.Utils;

namespace DProjects.Log.Storage.Tests {

    public class LogDeserializersTests : Base {

        //tests
        [Theory()]
        [InlineData("json:", """
            {"date":"2020-01-01T00:00:00.000Z","level":"Information","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"2020-01-01T00:00:00.000Z","level":"Warning","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"2020-01-01T00:00:00.000Z","level":"Error","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"2020-01-01T00:00:00.000Z","level":"Critical","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":["tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            """)]
        [InlineData("rat:", """
            2020-01-01T00:00:00.000Z [info|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [warn|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [error|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            2020-01-01T00:00:00.000Z [critical|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
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

    }
}
