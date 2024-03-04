using Xunit;
using System.IO;
using System.Text;
using DProjects.Log;
using DProjects.Utils;

namespace DProjects.Log.Tests {
    public class LogTextWriterTests : Base {

        //tests
        [Theory()]
        [InlineData("json:", """
            {"date":"*","level":"Information","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":[[]"tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"*","level":"Warning","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":[[]"tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"*","level":"Error","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":[[]"tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}
            {"date":"*","level":"Critical","message":"prefix1This is a message: 1, 2, False, True, hello","source":"source","user":"username1","tags":[[]"tag1","tag2"],"a1":1,"a2":2,"a3":false,"a4":true,"a5":"hello","messageOriginal":"This is a message: {a1}, {a2}, {a3}, {a4}, {a5}"}

            """)]
        [InlineData("rat:", """
            * [[]info|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            * [[]warn|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            * [[]error|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}
            * [[]critical|tag1|tag2] prefix1This is a message: 1, 2, False, True, hello | source: source | user: username1 | a1: 1 | a2: 2 | a3: False | a4: True | a5: hello | messageOriginal: This is a message: {a1}, {a2}, {a3}, {a4}, {a5}

            """)]
        [InlineData("raw:", """
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello
            prefix1This is a message: 1, 2, False, True, hello

            """)]
        public void WriterTest(string protocol, string expected) {
            var sw = new StringWriter();
            var serializer = mLogEntrySerializerFactoryByUrl.Create(protocol);
            using (var log = new LogTextWriter(sw, true, false, serializer)) { 
                var logClient = new LogClient(log);
                WriteSampleLogs(logClient);
            }   
            var result = sw.ToString();
            Assert.True(StringUtils.Like(result, expected));
        }


        //private
        private void WriteSampleLogs(ILogClient logClient) {
            logClient.Source = "source";
            logClient.Tags = ["tag1", "tag2"];
            logClient.User = "username1";
            logClient.Prefix = "prefix1";
            logClient.Debug("This is a message: {a1}, {a2}, {a3}, {a4}, {a5}", 1, 2, false, true, "hello");
            logClient.Info("This is a message: {a1}, {a2}, {a3}, {a4}, {a5}", 1, 2, false, true, "hello");
            logClient.Warning("This is a message: {a1}, {a2}, {a3}, {a4}, {a5}", 1, 2, false, true, "hello");
            logClient.Error("This is a message: {a1}, {a2}, {a3}, {a4}, {a5}", 1, 2, false, true, "hello");
            logClient.Critical("This is a message: {a1}, {a2}, {a3}, {a4}, {a5}", 1, 2, false, true, "hello");
        }
    }
}
