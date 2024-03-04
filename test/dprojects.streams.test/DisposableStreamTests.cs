using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class DisposableStreamTests
    {
        private bool disposed = false;

        private void OnDispose()
        {
            disposed = true;
        }

        [Fact()]
        public void DisposeTest()
        {
            var input = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var disposable = new DisposableStream(ms, OnDispose))
            {
                // Do nothing
            }

            Assert.True(disposed);
        }

        [Fact()]
        public void ReadTest()
        {
            var input = "Hello world";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var disposable = new DisposableStream(ms, OnDispose))
            {
                var buffer = new byte[1024];
                var bytesRead = disposable.Read(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact()]
        public async void ReadAsyncTest()
        {
            var input = "Hello world";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var disposable = new DisposableStream(ms, OnDispose))
            {
                var buffer = new byte[1024];
                var bytesRead = await disposable.ReadAsync(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }
    }
}
