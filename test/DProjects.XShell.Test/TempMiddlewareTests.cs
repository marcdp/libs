using System.Security.Cryptography;
using System.Text;

using DProjects.XShell.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace DProjects.XShell.Test {

    public sealed class TempMiddlewareTests {

        // methods
        [Fact]
        public async Task Upload_StoresExactBytesInOneGuidDirectoryAndReturnsTempUrl() {
            using var harness = new TempHarness();
            byte[] contents = [0, 1, 127, 128, 255];

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("report.txt", contents));

            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
            Assert.StartsWith("text/plain", response.ContentType);
            Assert.StartsWith("temp:/", response.Text);
            var directory = Assert.Single(Directory.GetDirectories(harness.Root));
            Assert.True(Guid.TryParse(Path.GetFileName(directory), out _));
            Assert.Equal(["report.txt"], Directory.GetFiles(directory).Select(Path.GetFileName));
            Assert.Equal(contents, File.ReadAllBytes(Path.Combine(directory, "report.txt")));
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task UploadUrl_ReportsSizeMimeHashAndFutureExpiration() {
            using var harness = new TempHarness();
            byte[] contents = [0, 1, 127, 128, 255];
            var before = DateTimeOffset.UtcNow.Add(TempHarness.Expiration).ToUnixTimeSeconds();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("report.txt", contents));

            var after = DateTimeOffset.UtcNow.Add(TempHarness.Expiration).ToUnixTimeSeconds();
            var url = new Uri(response.Text);
            var query = QueryHelpers.ParseQuery(url.Query);
            Assert.Equal(contents.Length.ToString(), query["size"].ToString());
            Assert.Equal("text/plain", query["type"].ToString());
            Assert.Equal(Convert.ToHexString(SHA256.HashData(contents)).ToLowerInvariant(), query["hash"].ToString());
            Assert.Matches("^[0-9a-f]{64}$", query["hash"].ToString());
            Assert.True(long.TryParse(query["expiration"], out var expiration));
            Assert.InRange(expiration, before, after);
        }
        [Fact]
        public async Task Upload_EscapesFilenameAndReturnedUrlCanBeDownloaded() {
            using var harness = new TempHarness();
            byte[] contents = Encoding.UTF8.GetBytes("file with spaces");

            var upload = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("my report.txt", contents));
            var url = new Uri(upload.Text);
            var download = await harness.SendAsync(HttpMethods.Get, "/temp" + url.AbsolutePath);

            Assert.Equal(StatusCodes.Status201Created, upload.StatusCode);
            Assert.Contains("my%20report.txt", upload.Text);
            Assert.Equal(["my report.txt"], Directory.GetFiles(Assert.Single(Directory.GetDirectories(harness.Root))).Select(Path.GetFileName));
            Assert.Equal(StatusCodes.Status200OK, download.StatusCode);
            Assert.Equal(contents, download.Body);
            Assert.Equal(0, harness.NextCalls);
        }
        [Theory]
        [InlineData(@"C:\fakepath\report.txt")]
        [InlineData("folder/report.txt")]
        public async Task Upload_StripsClientPathSegments(string clientFilename) {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile(clientFilename, [1, 2, 3]));

            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
            Assert.Equal("report.txt", Path.GetFileName(new Uri(response.Text).AbsolutePath));
            var directory = Assert.Single(Directory.GetDirectories(harness.Root));
            Assert.Equal(["report.txt"], Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Select(Path.GetFileName));
            Assert.Empty(Directory.GetDirectories(directory, "*", SearchOption.AllDirectories));
        }
        [Fact]
        public async Task NonFormPost_Returns415WithoutCreatingUpload() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", (UploadFile[]?)null);

            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task FormWithNoFiles_Returns400WithoutCreatingUpload() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", []);

            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
        }
        [Fact]
        public async Task FormWithMultipleFiles_Returns400WithoutCreatingUpload() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("one.txt", [1]), new UploadFile("two.txt", [2]));

            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
        }
        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        public async Task InvalidFilename_Returns400WithoutCreatingUpload(string filename) {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile(filename, [1]));

            Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
        }
        [Fact]
        public async Task Get_ReturnsUploadedBytesLengthAndContentType() {
            using var harness = new TempHarness();
            byte[] contents = [0, 1, 127, 128, 255];
            var upload = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("file.txt", contents));

            var response = await harness.SendAsync(HttpMethods.Get, "/temp" + new Uri(upload.Text).AbsolutePath);

            Assert.Equal(StatusCodes.Status201Created, upload.StatusCode);
            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Equal(contents, response.Body);
            Assert.Equal(contents.LongLength, response.ContentLength);
            Assert.StartsWith("text/plain", response.ContentType);
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task Get_UnknownExtensionUsesOctetStream() {
            using var harness = new TempHarness();
            var upload = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("file.xshellunknown", [1, 2]));

            var response = await harness.SendAsync(HttpMethods.Get, "/temp" + new Uri(upload.Text).AbsolutePath);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Equal("application/octet-stream", response.ContentType);
        }
        [Fact]
        public async Task Get_InvalidGuidReturns404() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Get, "/temp/not-a-guid/file.txt");

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task Get_MissingFileReturns404() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Get, "/temp/" + Guid.NewGuid().ToString("N") + "/missing.txt");

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Get_MalformedPathReturns404(bool extraSegment) {
            using var harness = new TempHarness();
            var path = "/temp/" + Guid.NewGuid().ToString("N") + (extraSegment ? "/file/extra" : "");

            var response = await harness.SendAsync(HttpMethods.Get, path);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        }
        [Theory]
        [InlineData("../secret.txt")]
        [InlineData("%2E%2E%2Fsecret.txt")]
        [InlineData("%2E%2E%5Csecret.txt")]
        public async Task Get_TraversalCannotReadOutsideGuidDirectory(string filename) {
            using var harness = new TempHarness();
            var id = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.Combine(harness.Root, id));
            File.WriteAllText(Path.Combine(harness.Root, "secret.txt"), "outside secret");
            File.WriteAllText(Path.Combine(harness.Root, id, "allowed.txt"), "allowed");

            var response = await harness.SendAsync(HttpMethods.Get, "/temp/" + id + "/" + filename);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.DoesNotContain("outside secret", response.Text);
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task OutsidePrefix_FallsThroughOnce() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Get, "/other/file");

            Assert.Equal(1, harness.NextCalls);
            Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
            Assert.Empty(response.Body);
            Assert.Null(response.ContentType);
        }
        [Fact]
        public async Task GetAtTempRoot_Returns404() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Get, "/temp");

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
        }
        [Fact]
        public async Task PostBelowTempRoot_Returns405() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Post, "/temp/something", new UploadFile("file.txt", [1]));

            Assert.Equal(StatusCodes.Status405MethodNotAllowed, response.StatusCode);
            Assert.Empty(Directory.GetFileSystemEntries(harness.Root));
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task UnsupportedMethodInsideTempRoot_Returns405() {
            using var harness = new TempHarness();

            var response = await harness.SendAsync(HttpMethods.Put, "/temp/" + Guid.NewGuid().ToString("N") + "/file.txt");

            Assert.Equal(StatusCodes.Status405MethodNotAllowed, response.StatusCode);
            Assert.Equal(0, harness.NextCalls);
        }
        [Fact]
        public async Task ConfiguredTrailingSlash_IsNormalizedForUpload() {
            using var harness = new TempHarness("/temp/");

            var response = await harness.SendAsync(HttpMethods.Post, "/temp", new UploadFile("file.txt", [1]));

            Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
            Assert.Equal(0, harness.NextCalls);
        }
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("/")]
        public void InvalidRequestRoot_IsRejected(string requestPath) {
            using var workspace = new TemporaryDirectory();

            Assert.Throws<ArgumentException>(() => new TempMiddleware(_ => Task.CompletedTask, new Extensions.TempConfig { Path = workspace.Root, BasePath = requestPath }));
        }

        // inner classes
        private sealed record UploadFile(string Filename, byte[] Contents);
        private sealed record MiddlewareResponse(int StatusCode, byte[] Body, string? ContentType, long? ContentLength) {

            // props
            public string Text => Encoding.UTF8.GetString(Body);
        }
        private sealed class TempHarness : IDisposable {

            // vars
            public static readonly TimeSpan Expiration = TimeSpan.FromHours(1);
            private readonly TemporaryDirectory mWorkspace = new();
            private readonly TempMiddleware mMiddleware;

            // props
            public string Root => mWorkspace.Root;
            public int NextCalls { get; private set; }

            // ctor
            public TempHarness(string requestPath = "/temp") {
                RequestDelegate next = context => {
                    NextCalls++;
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                };
                mMiddleware = new TempMiddleware(next, new Extensions.TempConfig { Path = Root, BasePath = requestPath, ExpirationTime = Expiration });
            }

            // methods
            public async Task<MiddlewareResponse> SendAsync(string method, string path, params UploadFile[]? files) {
                var context = new DefaultHttpContext();
                context.Request.Method = method;
                context.Request.Path = path;
                await using var body = new MemoryStream();
                context.Response.Body = body;
                var uploadStreams = new List<MemoryStream>();
                try {
                    // provide parsed multipart form files to the HTTP request
                    if (files != null) {
                        context.Request.ContentType = "multipart/form-data; boundary=test";
                        var formFiles = new FormFileCollection();
                        foreach (var file in files) {
                            var stream = new MemoryStream(file.Contents);
                            uploadStreams.Add(stream);
                            formFiles.Add(new FormFile(stream, 0, stream.Length, "file", file.Filename));
                        }
                        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), formFiles);
                    }

                    await mMiddleware.InvokeAsync(context);
                    return new MiddlewareResponse(context.Response.StatusCode, body.ToArray(), context.Response.ContentType, context.Response.ContentLength);
                } finally {
                    foreach (var stream in uploadStreams) stream.Dispose();
                }
            }
            public void Dispose() {
                mMiddleware.Dispose();
                mWorkspace.Dispose();
            }
        }
        private sealed class TemporaryDirectory : IDisposable {

            // props
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Test", Guid.NewGuid().ToString("N"));

            // methods
            public void Dispose() {
                if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            }
        }
    }
}
