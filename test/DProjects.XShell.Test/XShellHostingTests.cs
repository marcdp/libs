using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace DProjects.XShell.Test {

    public sealed class XShellHostingTests {

        // methods
        [Fact]
        public async Task AppBasePath_RedirectsToTrailingSlash() {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync("/app", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/app/", response.Headers.Location?.OriginalString);
        }
        [Fact]
        public async Task AppBaseRoot_ServesBootstrapHtml() {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync("/app/", TestContext.Current.CancellationToken);

            AssertBootstrap(response, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task ExplicitIndexAndAppBaseRoot_ServeSameBootstrapHtml() {
            await using var host = await HostingApplication.StartAsync();

            using var root = await host.Client.GetAsync("/app/", TestContext.Current.CancellationToken);
            using var index = await host.Client.GetAsync("/app/index.html", TestContext.Current.CancellationToken);
            var rootBody = await root.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var indexBody = await index.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AssertBootstrap(root, rootBody);
            AssertBootstrap(index, indexBody);
            Assert.Equal(rootBody, indexBody);
        }
        [Fact]
        public async Task ClientRoute_WithOrWithoutQuery_FallsBackToIndex() {
            await using var host = await HostingApplication.StartAsync();

            using var index = await host.Client.GetAsync("/app/index.html", TestContext.Current.CancellationToken);
            using var route = await host.Client.GetAsync("/app/customers/42", TestContext.Current.CancellationToken);
            using var queriedRoute = await host.Client.GetAsync("/app/customers/42?tab=details", TestContext.Current.CancellationToken);
            var indexBody = await index.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var routeBody = await route.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var queriedBody = await queriedRoute.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AssertBootstrap(route, routeBody);
            AssertBootstrap(queriedRoute, queriedBody);
            Assert.Equal(indexBody, routeBody);
            Assert.Equal(routeBody, queriedBody);
        }
        [Fact]
        public async Task ServiceWorker_UsesConfiguredXShellBasePath() {
            await using var host = await HostingApplication.StartAsync(xshellBasePath: "/custom/xshell");

            using var response = await host.Client.GetAsync("/app/sw.js", TestContext.Current.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/javascript", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("importScripts(\"/custom/xshell/sw.js\")", body);
            Assert.DoesNotContain("<!DOCTYPE html>", body, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public async Task XShellResource_IsServedBeforeAppBaseRedirect() {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync("/_resources/DProjects.XShell/xshell/bootstrap.js",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Content.Headers.ContentType?.MediaType is "text/javascript" or "application/javascript");
        }
        [Fact]
        public async Task TempRequest_IsHandledBeforeAppBaseRedirect() {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync("/temp/missing", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
        [Fact]
        public async Task RegisteredEndpoint_WinsOverSpaFallback() {
            await using var host = await HostingApplication.StartAsync(configureEndpoints: app => app.MapGet("/app/server", () => "server-response"));

            using var response = await host.Client.GetAsync("/app/server", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("server-response", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task RegisteredApiEndpoint_ExecutesUnderUnhandledPrefix() {
            await using var host = await HostingApplication.StartAsync(
                configureEndpoints: app => app.MapGet("/app/api/status", () => "api-response"));

            using var response = await host.Client.GetAsync("/app/api/status", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("api-response", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Theory]
        [InlineData("/app/api/missing")]
        [InlineData("/app/_internal/missing")]
        public async Task DefaultUnhandledPrefixes_BypassSpaFallback(string path) {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("xshell:app.basePath", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task CustomUnhandledPrefix_BypassesSpaFallback() {
            await using var host = await HostingApplication.StartAsync(unhandledPrefixes: ["/server"]);

            using var response = await host.Client.GetAsync("/app/server/missing", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("xshell:app.basePath", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task RequestOutsideAppBase_RedirectsToApp() {
            await using var host = await HostingApplication.StartAsync();

            using var response = await host.Client.GetAsync("/outside", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/app/", response.Headers.Location?.OriginalString);
        }
        [Fact]
        public async Task ResourcesBasePath_IsExemptFromAppRedirect() {
            await using var host = await HostingApplication.StartAsync(resourcesBase: "/resources-base");

            using var response = await host.Client.GetAsync("/resources-base/missing", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
        [Fact]
        public async Task RootMountedApp_ServesSpaAndPreservesUnhandledPaths() {
            await using var host = await HostingApplication.StartAsync(appBasePath: "");

            using var root = await host.Client.GetAsync("/", TestContext.Current.CancellationToken);
            using var route = await host.Client.GetAsync("/client-route", TestContext.Current.CancellationToken);
            using var api = await host.Client.GetAsync("/api/missing", TestContext.Current.CancellationToken);
            var rootBody = await root.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var routeBody = await route.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AssertBootstrap(root, rootBody);
            AssertBootstrap(route, routeBody);
            Assert.Equal(rootBody, routeBody);
            Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
            Assert.DoesNotContain("xshell:app.basePath", await api.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Bootstrap_ReflectsHostingConfiguration() {
            await using var host = await HostingApplication.StartAsync(appConfigPath: "/config/root.jsonc", xshellBasePath: "/custom/xshell");

            using var response = await host.Client.GetAsync("/app/index.html", TestContext.Current.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AssertBootstrap(response, body);
            Assert.Matches("name=\"xshell:app.basePath\"\\s+content=\"/app\"", body);
            Assert.Matches("name=\"xshell:app.configPath\"\\s+content=\"/config/root.jsonc\"", body);
            Assert.Matches("name=\"xshell:xshell.environment\"\\s+content=\"Development\"", body);
            Assert.Contains("src=\"/custom/xshell/bootstrap.js\"", body);
        }

        // methods (private)
        private static void AssertBootstrap(HttpResponseMessage response, string body) {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("xshell:app.basePath", body);
            Assert.Contains("/bootstrap.js", body);
        }

        // inner classes
        private sealed class HostingApplication : IAsyncDisposable {

            // vars
            private readonly WebApplication mApp;
            private readonly string mTempPath;

            // props
            public HttpClient Client { get; }

            // ctor
            private HostingApplication(WebApplication app, string tempPath) {
                mApp = app;
                mTempPath = tempPath;
                Client = app.GetTestClient();
            }

            // methods
            public static async Task<HostingApplication> StartAsync(string appBasePath = "/app", string resourcesBase = "",
                string[]? unhandledPrefixes = null, string appConfigPath = "", string xshellBasePath = "/_resources/DProjects.XShell/xshell",
                Action<WebApplication>? configureEndpoints = null) {
                var tempPath = Path.Combine(Path.GetTempPath(), "DProjects.XShell.Test", Guid.NewGuid().ToString("N"));
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
                builder.WebHost.UseTestServer();
                var app = builder.Build();
                try {
                    configureEndpoints?.Invoke(app);
                    app.UseXShell(new Extensions.Configuration {
                        Environment = "Development",
                        AppBasePath = appBasePath,
                        ResourcesBase = resourcesBase,
                        UnhandledPrefixes = unhandledPrefixes ?? new Extensions.Configuration().UnhandledPrefixes,
                        AppConfigPath = appConfigPath,
                        XShellBasePath = xshellBasePath,
                        TempPath = tempPath
                    });
                    await app.StartAsync(TestContext.Current.CancellationToken);
                    return new HostingApplication(app, tempPath);
                } catch {
                    await app.DisposeAsync();
                    if (Directory.Exists(tempPath)) Directory.Delete(tempPath, recursive: true);
                    throw;
                }
            }
            public async ValueTask DisposeAsync() {
                Client.Dispose();
                await mApp.DisposeAsync();
                if (Directory.Exists(mTempPath)) Directory.Delete(mTempPath, recursive: true);
            }
        }
    }
}
