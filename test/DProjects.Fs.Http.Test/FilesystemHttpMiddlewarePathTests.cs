using System.Text;
using System.Text.Json;
using DProjects.Factories;
using DProjects.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Fs.Http.Test {

    public class FilesystemHttpMiddlewarePathTests {

        // methods
        [Fact]
        public void OptionsPreserveDefaultsAndPositionalConstructorOrder() {
            // construct options through both supported constructor forms
            var defaults = new FilesystemHttpMiddleware.Options();
            var positional = new FilesystemHttpMiddleware.Options("/documents", "mem:");

            // verify defaults and the existing path-then-URL positional contract
            Assert.Equal("", defaults.FilesystemUrl);
            Assert.Equal("/", defaults.FilesystemPath);
            Assert.Equal("", defaults.UrlPath);
            Assert.Equal("/documents", positional.FilesystemPath);
            Assert.Equal("mem:", positional.FilesystemUrl);
            Assert.Equal("", positional.UrlPath);
        }
        [Fact]
        public async Task DefaultConfigurationMapsHttpRootToFilesystemRoot() {
            // arrange a file at the filesystem root and use the default path configuration
            var filesystem = CreateFilesystem();
            SaveText(filesystem, "/root.txt", "root");
            var options = CreateOptions();

            // request both forms of the HTTP root
            var root = await InvokeAsync(filesystem, options, "", HttpMethods.Get);
            var rootWithSlash = await InvokeAsync(filesystem, options, "/", HttpMethods.Get);
            var file = await InvokeAsync(filesystem, options, "/root.txt", HttpMethods.Get);

            // verify the default exposes the whole filesystem from the HTTP root
            Assert.Equal(HttpUtils.HTTP_OK, root.Context.Response.StatusCode);
            Assert.Equal(HttpUtils.HTTP_OK, rootWithSlash.Context.Response.StatusCode);
            Assert.Equal("root", file.Body);
        }
        [Theory]
        [InlineData("/files")]
        [InlineData("/files/")]
        [InlineData("/FILES")]
        public async Task UrlPathHandlesRootRequestsWithFrameworkPathCasing(string requestPath) {
            // arrange a filesystem mounted below a non-root URL path
            var filesystem = CreateFilesystem();
            var options = CreateOptions(urlPath: "/files");

            // request the exposed root using accepted path forms
            var result = await InvokeAsync(filesystem, options, requestPath, HttpMethods.Get);

            // verify segment matching and the HTTP-facing prefix response header
            Assert.Equal(HttpUtils.HTTP_OK, result.Context.Response.StatusCode);
            Assert.Equal("/files", result.Context.Response.Headers[FilesystemHttp.HEADER_FS_PATH_PREFIX].ToString());
            Assert.Equal(0, result.NextCalls);
        }
        [Theory]
        [InlineData("/filesOther")]
        [InlineData("/files-other")]
        [InlineData("/other/files")]
        public async Task UrlPathRejectsPartialAndUnrelatedSegments(string requestPath) {
            // arrange a middleware mounted at one complete URL segment
            var filesystem = CreateFilesystem();
            var options = CreateOptions(urlPath: "/files");

            // request a path outside that segment
            var result = await InvokeAsync(filesystem, options, requestPath, HttpMethods.Get);

            // verify the request continues through the pipeline unchanged
            Assert.Equal(HttpUtils.HTTP_NO_CONTENT, result.Context.Response.StatusCode);
            Assert.Equal(1, result.NextCalls);
        }
        [Fact]
        public async Task FilesystemPathMapsRequestsIntoConfiguredDirectory() {
            // arrange content inside and outside the configured filesystem directory
            var filesystem = CreateFilesystem("/documents");
            SaveText(filesystem, "/documents/report.txt", "inside");
            SaveText(filesystem, "/outside.txt", "outside");
            var options = CreateOptions(filesystemPath: "/documents");

            // request a path relative to the exposed filesystem directory
            var inside = await InvokeAsync(filesystem, options, "/report.txt", HttpMethods.Get);
            var outside = await InvokeAsync(filesystem, options, "/outside.txt", HttpMethods.Get);

            // verify only content rooted below FilesystemPath is visible
            Assert.Equal("inside", inside.Body);
            Assert.Equal(HttpUtils.HTTP_NOT_FOUND, outside.Context.Response.StatusCode);
        }
        [Fact]
        public async Task CustomUrlPathAndFilesystemPathMapTogether() {
            // arrange a nested file under the configured filesystem directory
            var filesystem = CreateFilesystem("/documents", "/documents/folder");
            SaveText(filesystem, "/documents/folder/a.txt", "mapped");
            var options = CreateOptions("/documents", "/files");

            // request the file through the configured HTTP URL path
            var result = await InvokeAsync(filesystem, options, "/files/folder/a.txt", HttpMethods.Get);

            // verify both prefixes are applied in their respective domains
            Assert.Equal(HttpUtils.HTTP_OK, result.Context.Response.StatusCode);
            Assert.Equal("mapped", result.Body);
        }
        [Theory]
        [InlineData("HEAD", 200)]
        [InlineData("OPTIONS", 204)]
        public async Task HeadAndOptionsUseMappedFilesystemPath(string method, int expectedStatusCode) {
            // arrange a file below a custom URL and filesystem mapping
            var filesystem = CreateFilesystem("/documents");
            SaveText(filesystem, "/documents/report.txt", "mapped");
            var options = CreateOptions("/documents", "/files");

            // invoke a metadata-oriented HTTP operation
            var result = await InvokeAsync(filesystem, options, "/files/report.txt", method);

            // verify the mapped entry is resolved
            Assert.Equal(expectedStatusCode, result.Context.Response.StatusCode);
        }
        [Fact]
        public async Task EntryAndDirectoryResponsesReturnPathsRelativeToFilesystemPath() {
            // arrange a directory and file below the configured filesystem root
            var filesystem = CreateFilesystem("/documents", "/documents/folder");
            SaveText(filesystem, "/documents/folder/a.txt", "a");
            var options = CreateOptions("/documents", "/files");

            // request one entry and a directory listing
            var entry = await InvokeAsync(filesystem, options, "/files/folder/a.txt", HttpMethods.Get, accept: FilesystemHttp.MIMETYPE_FS_ENTRY);
            var listing = await InvokeAsync(filesystem, options, "/files/folder", HttpMethods.Get, accept: FilesystemHttp.MIMETYPE_FS_ENTRIES);

            // verify response paths do not disclose the configured filesystem root
            using var entryJson = JsonDocument.Parse(entry.Body);
            using var listingJson = JsonDocument.Parse(listing.Body.Trim());
            Assert.Equal("/folder/a.txt", entryJson.RootElement.GetProperty("path").GetString());
            Assert.Equal("/folder/a.txt", listingJson.RootElement.GetProperty("path").GetString());
        }
        [Fact]
        public async Task FileLifecycleStaysWithinMappedFilesystemDirectory() {
            // arrange a writable middleware mapped to a non-root filesystem directory
            var filesystem = CreateFilesystem("/documents");
            var options = CreateOptions("/documents", "/files", FilesystemHttpMiddleware.Modes.ReadWrite);

            // create, replace, read, and delete a file through the HTTP mapping
            var created = await InvokeAsync(filesystem, options, "/files/new.txt", HttpMethods.Put, "first");
            var modified = await InvokeAsync(filesystem, options, "/files/new.txt", HttpMethods.Put, "second");
            var read = await InvokeAsync(filesystem, options, "/files/new.txt", HttpMethods.Get);
            var deleted = await InvokeAsync(filesystem, options, "/files/new.txt", HttpMethods.Delete);

            // verify the lifecycle targets the mapped filesystem path only
            Assert.Equal(HttpUtils.HTTP_CREATED, created.Context.Response.StatusCode);
            Assert.Equal(HttpUtils.HTTP_OK, modified.Context.Response.StatusCode);
            Assert.Equal("second", read.Body);
            Assert.Equal(HttpUtils.HTTP_OK, deleted.Context.Response.StatusCode);
            Assert.Null(filesystem.GetEntry("/documents/new.txt"));
            Assert.Null(filesystem.GetEntry("/new.txt"));
        }
        [Fact]
        public async Task CopyMoveAndSyncSourcesUseMappedHttpPaths() {
            // arrange source entries and sync directories below the configured filesystem root
            var filesystem = CreateFilesystem("/documents", "/documents/sync-source", "/documents/sync-destination");
            SaveText(filesystem, "/documents/copy-source.txt", "copy");
            SaveText(filesystem, "/documents/move-source.txt", "move");
            SaveText(filesystem, "/documents/sync-source/synced.txt", "sync");
            var options = CreateOptions("/documents", "/files", FilesystemHttpMiddleware.Modes.ReadWrite);

            // submit operation source paths in the middleware's HTTP namespace
            var copyBody = JsonSerializer.Serialize(new FilesystemHttp.CopyRequest { Source = "/files/copy-source.txt" });
            var moveBody = JsonSerializer.Serialize(new FilesystemHttp.MoveRequest { Source = "/files/move-source.txt" });
            var syncBody = JsonSerializer.Serialize(new FilesystemHttp.SyncRequest { Source = "/files/sync-source", Mode = SyncModes.LeftToRight });
            var copy = await InvokeAsync(filesystem, options, "/files/copied.txt", HttpMethods.Put, copyBody, FilesystemHttp.MIMETYPE_FS_COPY);
            var move = await InvokeAsync(filesystem, options, "/files/moved.txt", HttpMethods.Put, moveBody, FilesystemHttp.MIMETYPE_FS_MOVE);
            var sync = await InvokeAsync(filesystem, options, "/files/sync-destination", HttpMethods.Put, syncBody, FilesystemHttp.MIMETYPE_FS_SYNC);

            // verify every source and destination remained under FilesystemPath
            Assert.Equal(HttpUtils.HTTP_CREATED, copy.Context.Response.StatusCode);
            Assert.Equal(HttpUtils.HTTP_CREATED, move.Context.Response.StatusCode);
            Assert.Equal(HttpUtils.HTTP_OK, sync.Context.Response.StatusCode);
            Assert.NotNull(filesystem.GetEntry("/documents/copied.txt"));
            Assert.NotNull(filesystem.GetEntry("/documents/moved.txt"));
            Assert.Null(filesystem.GetEntry("/documents/move-source.txt"));
            Assert.NotNull(filesystem.GetEntry("/documents/sync-destination/synced.txt"));
        }
        [Theory]
        [InlineData("/files/../escaped.txt")]
        [InlineData("/files/%2e%2e/escaped.txt")]
        [InlineData("/files/%252e%252e/escaped.txt")]
        [InlineData("/files/folder%2f..%2f..%2fescaped.txt")]
        [InlineData("/files/%5c..%5cescaped.txt")]
        public async Task TraversalRequestsAreRejectedBeforeFilesystemAccess(string requestPath) {
            // arrange a writable middleware with a protected filesystem root
            var filesystem = CreateFilesystem("/documents");
            var options = CreateOptions("/documents", "/files", FilesystemHttpMiddleware.Modes.ReadWrite);

            // attempt to create a file through plain and encoded traversal paths
            var result = await InvokeAsync(filesystem, options, requestPath, HttpMethods.Put, "blocked");

            // verify the invalid path is rejected and no file escapes the configured root
            Assert.Equal(HttpUtils.HTTP_BAD_REQUEST, result.Context.Response.StatusCode);
            Assert.Null(filesystem.GetEntry("/escaped.txt"));
            Assert.Null(filesystem.GetEntry("/documents/escaped.txt"));
        }
        [Fact]
        public async Task TraversalInOperationSourceIsRejected() {
            // arrange a source outside the configured filesystem root
            var filesystem = CreateFilesystem("/documents");
            SaveText(filesystem, "/secret.txt", "secret");
            var options = CreateOptions("/documents", "/files", FilesystemHttpMiddleware.Modes.ReadWrite);
            var body = JsonSerializer.Serialize(new FilesystemHttp.CopyRequest { Source = "/files/%2e%2e/secret.txt" });

            // attempt to copy the outside source into the exposed directory
            var result = await InvokeAsync(filesystem, options, "/files/copied.txt", HttpMethods.Put, body, FilesystemHttp.MIMETYPE_FS_COPY);

            // verify the source mapping cannot bypass FilesystemPath
            Assert.Equal(HttpUtils.HTTP_BAD_REQUEST, result.Context.Response.StatusCode);
            Assert.Null(filesystem.GetEntry("/documents/copied.txt"));
        }
        [Theory]
        [InlineData("")]
        [InlineData("relative.txt")]
        [InlineData("/other/secret.txt")]
        public async Task InvalidOrOutsideOperationSourcesAreRejected(string sourcePath) {
            // arrange a writable mapped middleware and a copy request with an invalid HTTP source path
            var filesystem = CreateFilesystem("/documents");
            var options = CreateOptions("/documents", "/files", FilesystemHttpMiddleware.Modes.ReadWrite);
            var body = JsonSerializer.Serialize(new FilesystemHttp.CopyRequest { Source = sourcePath });

            // submit the invalid source path
            var result = await InvokeAsync(filesystem, options, "/files/copied.txt", HttpMethods.Put, body, FilesystemHttp.MIMETYPE_FS_COPY);

            // verify malformed or outside sources cannot reach the filesystem
            Assert.Equal(HttpUtils.HTTP_BAD_REQUEST, result.Context.Response.StatusCode);
            Assert.Null(filesystem.GetEntry("/documents/copied.txt"));
        }

        // methods (private)
        private static FilesystemHttpMiddleware.Options CreateOptions(string filesystemPath = "/", string urlPath = "",
            FilesystemHttpMiddleware.Modes mode = FilesystemHttpMiddleware.Modes.ReadOnly) {
            // create standard anonymous middleware options for path behavior tests
            return new FilesystemHttpMiddleware.Options {
                FilesystemUrl = "mem:",
                FilesystemPath = filesystemPath,
                UrlPath = urlPath,
                AllowAnonymous = true,
                Mode = mode
            };
        }
        private static FilesystemMem CreateFilesystem(params string[] directories) {
            // create an in-memory filesystem with the requested directory structure
            var filesystem = new FilesystemMem(false, false);
            foreach (string directory in directories) filesystem.CreateDirectory(directory);
            return filesystem;
        }
        private static void SaveText(IFilesystem filesystem, string path, string value) {
            // store deterministic UTF-8 test content
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            filesystem.SaveFile(path, stream, new SaveFileSettings());
        }
        private static async Task<(DefaultHttpContext Context, string Body, int NextCalls)> InvokeAsync(IFilesystem filesystem,
            FilesystemHttpMiddleware.Options options, string path, string method, string body = "", string? contentType = null, string? accept = null) {
            // configure the middleware and its standard authorization dependency
            await using var serviceProvider = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
            var factory = new TestFilesystemFactory(filesystem);
            int nextCalls = 0;
            RequestDelegate next = context => {
                nextCalls++;
                context.Response.StatusCode = HttpUtils.HTTP_NO_CONTENT;
                return Task.CompletedTask;
            };
            using var middleware = new FilesystemHttpMiddleware(next, factory, options);

            // create an isolated HTTP context for the request
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            var context = new DefaultHttpContext {
                RequestServices = serviceProvider
            };
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.ContentType = contentType;
            context.Request.ContentLength = bodyBytes.Length;
            context.Request.Body = new MemoryStream(bodyBytes);
            if (accept != null) context.Request.Headers.Accept = accept;
            context.Response.Body = new MemoryStream();

            // invoke and capture the response body
            var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
            await middleware.Invoke(context, NullLogger<IFilesystem>.Instance, authorizationService);
            context.Response.Body.Position = 0;
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            string responseBody = await reader.ReadToEndAsync();
            return (context, responseBody, nextCalls);
        }

        private sealed class TestFilesystemFactory : IFactoryByUrl<IFilesystem> {

            // vars
            private readonly IFilesystem mFilesystem;

            // ctor
            public TestFilesystemFactory(IFilesystem filesystem) {
                mFilesystem = filesystem;
            }

            // methods
            public IFilesystem Create(string url) {
                return mFilesystem;
            }
        }
    }
}