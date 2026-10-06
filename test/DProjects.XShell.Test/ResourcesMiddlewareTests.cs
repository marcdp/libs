using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using DProjects.XShell.Middlewares;
using DProjects.XShell.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DProjects.XShell.Test {

    public sealed class ResourcesMiddlewareTests {

        // consts
        private const string ModuleDescriptor = """
            { "modules": { "demo": { "defaults": { "page": { "renderEngine": "x" } } } } }
            """;

        // methods
        [Fact]
        public async Task ExistingStaticResource_IsServedWithoutCallingNext() {
            using var workspace = new TemporaryDirectory();
            byte[] contents = [0, 1, 127, 128, 255];
            workspace.Write("resources/module/file.txt", contents);

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/module/file.txt");

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Equal(contents, response.Body);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task RequestOutsideResourcePath_FallsThroughOnce() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/module/file.txt", "resource");

            var response = await SendAsync(workspace.PathFor("resources"), "/other/file.txt");

            Assert.Equal(1, response.NextCalls);
            Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
            Assert.Empty(response.Body);
            Assert.Null(response.ContentType);
        }
        [Theory]
        [InlineData("module.jsonc", "application/json")]
        [InlineData("readme.md", "text/markdown")]
        public async Task XShellStaticExtensions_UseExpectedContentType(string fileName, string contentType) {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/" + fileName, "content");

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/" + fileName);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.StartsWith(contentType, response.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task StaticResource_UsesNoCacheHeadersOnlyInDevelopment() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/file.txt", "content");

            var development = await SendAsync(workspace.PathFor("resources"), "/_resources/file.txt", isDevelopment: true);
            var production = await SendAsync(workspace.PathFor("resources"), "/_resources/file.txt");

            Assert.Equal(StatusCodes.Status200OK, development.StatusCode);
            AssertNoCache(development);
            Assert.Equal(StatusCodes.Status200OK, production.StatusCode);
            Assert.False(production.Headers.ContainsKey("Cache-Control"));
            Assert.False(production.Headers.ContainsKey("Pragma"));
            Assert.False(production.Headers.ContainsKey("Expires"));
        }
        [Fact]
        public async Task DevelopmentModuleFilesJson_IsGeneratedForContainingModule() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            workspace.Write("resources/demo/a.txt", "alpha");
            workspace.Write("resources/demo/nested/b.txt", "beta");
            var moduleDirectory = workspace.PathFor("resources/demo");
            Assert.False(File.Exists(Path.Combine(moduleDirectory, FilesIndexer.ModuleFilesJson)));

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/module.files.json", isDevelopment: true);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.StartsWith("application/json", response.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, response.NextCalls);
            AssertNoCache(response);
            var actual = ReadInventory(response.Text);
            var expected = ReadInventory(await new FilesIndexer().CreateJsonAsync(moduleDirectory, TestContext.Current.CancellationToken));
            Assert.Equal(expected, actual);
            Assert.Equal(["/a.txt", "/module.jsonc", "/nested/b.txt"], actual.Select(file => file.Path));
            Assert.DoesNotContain(actual, file => file.Path.EndsWith("/module.files.json", StringComparison.OrdinalIgnoreCase));
            var a = Assert.Single(actual, file => file.Path == "/a.txt");
            Assert.Equal(Encoding.UTF8.GetByteCount("alpha"), a.Size);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("alpha"))).ToLowerInvariant(), a.Hash);
        }
        [Fact]
        public async Task VirtualInventory_RequiresDescriptor() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/a.txt", "content");

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/module.files.json", isDevelopment: true);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Empty(response.Body);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task VirtualInventory_MissingDirectoryReturns404() {
            using var workspace = new TemporaryDirectory();
            Directory.CreateDirectory(workspace.PathFor("resources"));

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/missing/module.files.json", isDevelopment: true);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task Production_DoesNotGenerateVirtualInventory() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/module.files.json");

            Assert.Equal(1, response.NextCalls);
            Assert.Equal(StatusCodes.Status204NoContent, response.StatusCode);
            Assert.Empty(response.Body);
        }
        [Fact]
        public async Task DevelopmentHtmlSfc_IsCompiledAtJavaScriptPath() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            workspace.Write("resources/demo/component.html", "<template><p>Hello</p></template>");

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/component.js", isDevelopment: true);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.StartsWith("application/javascript", response.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("template: `<p>Hello</p>`", response.Text);
            Assert.Contains("templateRenderer:", response.Text);
            AssertNoCache(response);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task DevelopmentHtmlSfc_IsNotServedAtAuthoredPath() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            workspace.Write("resources/demo/component.html", "<template><p>Hello</p></template>");

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/component.html", isDevelopment: true);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Empty(response.Body);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task DevelopmentRuntimePathCollision_IsRejected() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            workspace.Write("resources/demo/component.js", "export default {};");
            workspace.Write("resources/demo/component.html", "<template><p>Hello</p></template>");

            var root = workspace.PathFor("resources");
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(root, "/_resources/demo/component.js", isDevelopment: true));

            Assert.Contains("component.js", exception.Message);
            Assert.Contains("component.html", exception.Message);
            Assert.Contains("/demo/component.js", exception.Message);
        }
        [Fact]
        public async Task DevelopmentJavaScript_CompilesResourceReferences() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            const string source = "export default { meta: { renderEngine: 'html' }, style: `.card { background: url('./image.png'); }` };";
            workspace.Write("resources/demo/component.js", source);

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/component.js", isDevelopment: true);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.StartsWith("application/javascript", response.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("url('/image.png')", response.Text);
            Assert.DoesNotContain("url('./image.png')", response.Text);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task DevelopmentCss_CompilesRelativeResourceUrl() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("resources/demo/module.jsonc", ModuleDescriptor);
            workspace.Write("resources/demo/styles/site.css", ".card { background: url('../image.png'); }");

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/demo/styles/site.css", isDevelopment: true);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.StartsWith("text/css", response.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("url('/image.png')", response.Text);
            AssertNoCache(response);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task DevelopmentResourceWithoutModuleDescriptor_UsesStaticFileStage() {
            using var workspace = new TemporaryDirectory();
            const string source = "export default { style: `url('./image.png')` };";
            workspace.Write("resources/orphan.js", source);
            workspace.Write("module.jsonc", ModuleDescriptor);

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/orphan.js", isDevelopment: true);

            Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
            Assert.Equal(source, response.Text);
            Assert.Equal(0, response.NextCalls);
        }
        [Fact]
        public async Task DevelopmentMissingCompiledResource_Returns404() {
            using var workspace = new TemporaryDirectory();
            Directory.CreateDirectory(workspace.PathFor("resources"));

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/missing.js", isDevelopment: true);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Equal(0, response.NextCalls);
        }
        [Theory]
        [InlineData("../outside.txt")]
        [InlineData("../outside.js")]
        public async Task Traversal_DoesNotServeFileOutsideResourceRoot(string relativePath) {
            using var workspace = new TemporaryDirectory();
            workspace.Write("outside.txt", "outside secret");
            workspace.Write("outside.js", "outside secret");
            Directory.CreateDirectory(workspace.PathFor("resources"));

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/" + relativePath, isDevelopment: true);

            Assert.True(response.StatusCode is StatusCodes.Status404NotFound or StatusCodes.Status204NoContent);
            Assert.DoesNotContain("outside secret", response.Text);
        }
        [Fact]
        public async Task VirtualInventoryTraversal_DoesNotIndexDirectoryOutsideResourceRoot() {
            using var workspace = new TemporaryDirectory();
            workspace.Write("outside/module.jsonc", ModuleDescriptor);
            workspace.Write("outside/secret.txt", "outside secret");
            Directory.CreateDirectory(workspace.PathFor("resources"));

            var response = await SendAsync(workspace.PathFor("resources"), "/_resources/../outside/module.files.json", isDevelopment: true);

            Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
            Assert.Empty(response.Body);
            Assert.Equal(0, response.NextCalls);
        }

        // methods (private)
        private static async Task<MiddlewareResponse> SendAsync(string resourceRoot, string requestPath, bool isDevelopment = false) {
            var serviceCollection = new ServiceCollection().AddLogging();
            serviceCollection.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(resourceRoot, isDevelopment));
            using var services = serviceCollection.BuildServiceProvider();
            var nextCalls = 0;
            RequestDelegate next = context => {
                nextCalls++;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            };
            var middleware = new ResourcesMiddleware(next, services, resourceRoot, "/_resources", isDevelopment);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = requestPath;
            await using var body = new MemoryStream();
            context.Response.Body = body;

            await middleware.InvokeAsync(context);

            return new MiddlewareResponse(context.Response.StatusCode, body.ToArray(), context.Response.ContentType,
                context.Response.Headers, nextCalls);
        }
        private static void AssertNoCache(MiddlewareResponse response) {
            var directives = response.Headers.CacheControl.ToString().Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Assert.Contains("no-cache", directives);
            Assert.Contains("no-store", directives);
            Assert.Contains("must-revalidate", directives);
            Assert.Equal("no-cache", response.Headers.Pragma.ToString());
            Assert.Equal("0", response.Headers.Expires.ToString());
        }
        private static InventoryEntry[] ReadInventory(string json) {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.EnumerateArray().Select(item => new InventoryEntry(
                item.GetProperty("path").GetString()!, item.GetProperty("size").GetInt64(), item.GetProperty("hash").GetString()!
            )).ToArray();
        }

        // inner classes
        private sealed record InventoryEntry(string Path, long Size, string Hash);
        private sealed record MiddlewareResponse(int StatusCode, byte[] Body, string? ContentType, IHeaderDictionary Headers, int NextCalls) {

            // props
            public string Text => Encoding.UTF8.GetString(Body);
        }
        private sealed class TestWebHostEnvironment : IWebHostEnvironment {

            // props
            public string ApplicationName { get; set; } = "DProjects.XShell.Test";
            public string EnvironmentName { get; set; } = "";
            public string ContentRootPath { get; set; } = "";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
            public string WebRootPath { get; set; } = "";
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

            // ctor
            public TestWebHostEnvironment(string root, bool isDevelopment) {
                EnvironmentName = isDevelopment ? "Development" : "Production";
                ContentRootPath = root;
                WebRootPath = root;
            }
        }
        private sealed class TemporaryDirectory : IDisposable {

            // props
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Test", Guid.NewGuid().ToString("N"));

            // methods
            public string PathFor(string name) => Path.Combine(Root, name.Replace('/', Path.DirectorySeparatorChar));
            public void Write(string name, string contents) => Write(name, Encoding.UTF8.GetBytes(contents));
            public void Write(string name, byte[] contents) {
                var path = PathFor(name);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, contents);
            }
            public void Dispose() {
                if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            }
        }
    }
}
