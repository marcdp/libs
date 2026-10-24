using System.Security.Claims;
using DProjects.Factories;
using DProjects.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Fs.Http.Test {

    public class FilesystemHttpAuthorizationTests {

        // consts
        private const string POLICY_NAME = "FilesystemAccess";
        private const string PREFIX = "/files";

        // methods
        [Theory]
        [InlineData(true, false, "", false, HttpUtils.HTTP_OK)]
        [InlineData(true, false, POLICY_NAME, false, HttpUtils.HTTP_OK)]
        [InlineData(false, false, "", false, HttpUtils.HTTP_UNAUTHORIZED)]
        [InlineData(false, false, POLICY_NAME, true, HttpUtils.HTTP_UNAUTHORIZED)]
        [InlineData(false, true, "", false, HttpUtils.HTTP_OK)]
        [InlineData(false, true, POLICY_NAME, true, HttpUtils.HTTP_OK)]
        [InlineData(false, true, POLICY_NAME, false, HttpUtils.HTTP_FORBIDDEN)]
        public async Task AuthorizationMatrix(bool allowAnonymous, bool authenticated, string authorizationPolicy, bool policySucceeds,
            int expectedStatusCode) {
            // invoke the middleware for each authentication and authorization combination
            var result = await InvokeMiddlewareAsync(allowAnonymous, authenticated, authorizationPolicy, policySucceeds, PREFIX, HttpMethods.Get);

            // verify rejected requests stop before filesystem access
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.Equal(expectedStatusCode == HttpUtils.HTTP_OK ? 1 : 0, result.FilesystemOperations);
            Assert.Equal(0, result.NextCalls);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task MissingAuthorizationPolicyRequiresAuthenticationOnly(string? authorizationPolicy) {
            // invoke with an authenticated principal and no effective policy
            var result = await InvokeMiddlewareAsync(false, true, authorizationPolicy!, false, PREFIX, HttpMethods.Get);

            // verify the existing authentication-only behavior is retained
            Assert.Equal(HttpUtils.HTTP_OK, result.StatusCode);
            Assert.Equal(1, result.FilesystemOperations);
        }
        [Fact]
        public async Task RequestOutsidePrefixBypassesAuthorization() {
            // invoke without authentication against a path outside the middleware prefix
            var result = await InvokeMiddlewareAsync(false, false, POLICY_NAME, false, "/outside", HttpMethods.Get);

            // verify the next middleware handles the request without filesystem access
            Assert.Equal(HttpUtils.HTTP_NO_CONTENT, result.StatusCode);
            Assert.Equal(0, result.FilesystemOperations);
            Assert.Equal(1, result.NextCalls);
        }
        [Fact]
        public async Task AuthorizationFailurePreventsFilesystemOperations() {
            // invoke a mutating request with an authenticated principal that fails the policy
            var result = await InvokeMiddlewareAsync(false, true, POLICY_NAME, false, PREFIX + "/blocked", HttpMethods.Put,
                FilesystemHttp.MIMETYPE_FS_ENTRY_DIRECTORY);

            // verify policy failure returns forbidden before any filesystem operation
            Assert.Equal(HttpUtils.HTTP_FORBIDDEN, result.StatusCode);
            Assert.Equal(0, result.FilesystemOperations);
            Assert.Equal(0, result.NextCalls);
        }

        // methods (private)
        private static async Task<(int StatusCode, int FilesystemOperations, int NextCalls)> InvokeMiddlewareAsync(bool allowAnonymous,
            bool authenticated, string authorizationPolicy, bool policySucceeds, string path, string method, string? contentType = null) {
            // configure standard ASP.NET Core authorization with a resource-aware policy
            await using var serviceProvider = CreateServiceProvider();
            var filesystem = new CountingFilesystem();
            var factory = new TestFilesystemFactory(filesystem);
            var nextCalls = 0;
            RequestDelegate next = context => {
                nextCalls++;
                context.Response.StatusCode = HttpUtils.HTTP_NO_CONTENT;
                return Task.CompletedTask;
            };
            var options = new FilesystemHttpMiddleware.Options("/", "mem:") {
                AllowAnonymous = allowAnonymous,
                AuthorizationPolicy = authorizationPolicy,
                Mode = FilesystemHttpMiddleware.Modes.ReadWrite,
                UrlPath = PREFIX
            };
            using var middleware = new FilesystemHttpMiddleware(next, factory, options);

            // create the request principal and context
            var claims = policySucceeds ? new[] { new Claim("permission", "allowed") } : [];
            var identity = authenticated ? new ClaimsIdentity(claims, "Test") : new ClaimsIdentity(claims);
            var context = new DefaultHttpContext {
                User = new ClaimsPrincipal(identity)
            };
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.ContentType = contentType;

            // invoke using services resolved from the standard dependency injection container
            await middleware.Invoke(context, NullLogger<IFilesystem>.Instance);
            return (context.Response.StatusCode, filesystem.OperationCalls, nextCalls);
        }
        private static ServiceProvider CreateServiceProvider() {
            // register a policy that verifies both the principal and HttpContext authorization resource
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(options => {
                options.AddPolicy(POLICY_NAME, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(authorizationContext => authorizationContext.User.HasClaim("permission", "allowed")
                        && authorizationContext.Resource is HttpContext httpContext && httpContext.Request.Path.StartsWithSegments(PREFIX));
                });
            });
            return services.BuildServiceProvider();
        }

        private sealed class CountingFilesystem : FilesystemMem {

            // props
            public int OperationCalls { get; private set; }

            // ctor
            public CountingFilesystem() : base(false, false) {
            }

            // methods
            public override Entry? GetEntry(string path) {
                OperationCalls++;
                return base.GetEntry(path);
            }
            public override Entry CreateDirectory(string path) {
                OperationCalls++;
                return base.CreateDirectory(path);
            }
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