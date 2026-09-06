using Xunit;
using System.IO;
using System.Xml;
using DProjects.Text.Readers;

namespace DProjects.Text.Readers.Tests
{
    public class XmlDocumentsReaderTests
    {
        [Fact()]
        public void ReadTest()
        {
            using (var sr = new StringReader("<root><child>Test</child></root>"))
            using (var reader = new XmlDocumentsReader(sr))
            {
                var result = reader.Read();
                Assert.NotNull(result);
                Assert.Equal("root", result.DocumentElement!.Name);
                Assert.Equal("Test", result.DocumentElement!.FirstChild!.InnerText);
            }
        }

        [Fact()]
        public async Task ReadAsyncTest()
        {
            using (var sr = new StringReader("<root><child>Test</child></root>"))
            using (var reader = new XmlDocumentsReader(sr))
            {
                var result = await reader.ReadAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(result);
                Assert.Equal("root", result.DocumentElement!.Name);
                Assert.Equal("Test", result.DocumentElement!.FirstChild!.InnerText);
            }
        }

        [Fact()]
        public void DisposeTest()
        {
            var sr = new StringReader("<root><child>Test</child></root>");
            var reader = new XmlDocumentsReader(sr);
            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sr.Peek());
        }

    }
}
