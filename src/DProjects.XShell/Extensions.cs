
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using DProjects.Utils;

namespace DProjects.XShell {

    public static class Extensions {


        // inner class
        public class Configuration {
            public AppConfig App { get; init; } = new AppConfig();
            public TempConfig Temp { get; init; } = new TempConfig();
            public XShellConfig XShell { get; init; } = new XShellConfig();
            public ResourcesConfig Resources { get; init; } = new ResourcesConfig();
            public ServerConfig Server { get; init; } = new ServerConfig();            
        }
        public class ResourcesConfig {
            public string BasePath { get; init; } = "";
        }
        public class XShellConfig {
            public string BasePath { get; init; } = "";
        }
        public class AppConfig {
            public string Description { get; init; } = "";
            public string BasePath { get; init; } = "";
            public string ConfigPath { get; init; } = "";
            public Dictionary<string, string> Params { get; init; } = new();
        }
        public class ServerConfig {
            public string? Environment { get; init; } = null;
            public string HeaderCSP { get; init; } = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; ";
            public string[] UnhandledPrefixes { get; init; } = new string[] { "/_", "/api", "/temp" };
        }
        public class TempConfig {
            public string Path { get; init; } = "";
            public string BasePath { get; init; } = "/temp";
            public TimeSpan ExpirationTime { get; init; } = TimeSpan.FromHours(1);
            public long FileSizeLimit { get; init; } = 100 * 1024 * 1024; // 100 MB
        }


        // constants
        public const string ResourceName = "DProjects.XShell";


        // methods
        public static void UseXShell(this WebApplication app, Configuration config) {

            // validate static host configuration before resolving resources or registering middleware
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(config);
            ValidateConfiguration(config);

            // config webapplication
            var assembly = typeof(Extensions).Assembly;
            var environment = (string.IsNullOrEmpty(config.Server.Environment) ? app.Environment.EnvironmentName : config.Server.Environment);
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            string resourcePath;
            if (isDevelopment) {
                var projectDirectory = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "ProjectDirectory")?.Value;
                if (string.IsNullOrEmpty(projectDirectory)) {
                    throw new InvalidOperationException("Unable to determine the DProjects.XShell project directory.");
                }
                resourcePath = Path.Combine(projectDirectory, "Resources", ResourceName);
            } else {
                var assemblyPath = Path.GetDirectoryName(assembly.Location)!;
                resourcePath = Path.Combine(assemblyPath, "Resources", ResourceName);
            }
            if (!Directory.Exists(resourcePath)) throw new DirectoryNotFoundException($"XShell resources directory not found: {resourcePath}");

            // /_resources
            app.UseMiddleware<Middlewares.ResourcesMiddleware>(resourcePath, config.Resources.BasePath, isDevelopment);

            // /_temp
            app.UseMiddleware<Middlewares.TempMiddleware>(config.Temp);

            // redirect canonical base URL
            app.Use(async (context, next) => {
                if (context.Request.Path == config.App.BasePath) {
                    context.Response.Redirect(config.App.BasePath + "/");
                    return;
                } else if (config.App.BasePath.Length > 0 && !context.Request.Path.StartsWithSegments(config.App.BasePath)) {
                    if (config.App.BasePath.Length > 0 && context.Request.Path.StartsWithSegments(config.App.BasePath)) {
                    } else {
                        context.Response.Redirect(config.App.BasePath + "/");
                        return;

                    }
                }
                await next();
            });

            // select registered endpoints
            app.UseRouting();

            // bootstrap files /
            var bootstrapFiles = (new Services.BoostrapFilesBuilder()).Build(config, environment);

            // SPA fallback
            app.Use(async (context, next) => {

                // bootstrap files
                foreach (var aa in bootstrapFiles.Keys) {
                    if (context.Request.Path == config.App.BasePath + aa) {
                        context.Response.ContentType = MimeTypeUtils.GetMimeType(context.Request.Path);
                        await context.Response.WriteAsync(bootstrapFiles[aa]);
                        return;
                    }
                }

                // if routing already found an endpoint, let it handle the request
                if (context.GetEndpoint() != null) {
                    await next();
                    return;
                }

                // only handle requests inside app.basePath
                if (!context.Request.Path.StartsWithSegments(config.App.BasePath, out var remaining)) {
                    await next();
                    return;
                }

                var slug = remaining.Value ?? "";

                // reserved prefixes must continue through the pipeline
                foreach (var unhandledPrefix in config.Server.UnhandledPrefixes) {
                    if (slug.StartsWith(unhandledPrefix, StringComparison.OrdinalIgnoreCase)) {
                        await next();
                        return;
                    }
                }

                // SPA fallback
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(bootstrapFiles["/index.html"]);
            });


        }

        // methods (private)
        private static void ValidateConfiguration(Configuration config) {
            if (config.App == null) throw new ArgumentNullException(nameof(config.App), "XShell App configuration is required.");
            if (config.Temp == null) throw new ArgumentNullException(nameof(config.Temp), "XShell Temp configuration is required.");
            if (config.XShell == null) throw new ArgumentNullException(nameof(config.XShell), "XShell XShell configuration is required.");
            if (config.Resources == null) throw new ArgumentNullException(nameof(config.Resources), "XShell Resources configuration is required.");
            if (config.Server == null) throw new ArgumentNullException(nameof(config.Server), "XShell Server configuration is required.");
            if (config.App.Description == null) throw new ArgumentNullException(nameof(config.App.Description), "XShell App.Description must not be null.");
            if (config.App.Params == null) throw new ArgumentNullException(nameof(config.App.Params), "XShell App.Params must not be null.");

            // validate required paths and numeric Temp limits
            if (string.IsNullOrWhiteSpace(config.App.ConfigPath)) throw new ArgumentException("XShell App.ConfigPath must be a non-empty application configuration path or URL.", nameof(config));
            if (string.IsNullOrWhiteSpace(config.Temp.Path)) throw new ArgumentException("XShell Temp.Path must be a non-empty physical storage path.", nameof(config));
            try {
                Path.GetFullPath(config.Temp.Path);
            } catch (ArgumentException exception) {
                throw new ArgumentException("XShell Temp.Path must be a valid physical storage path.", nameof(config), exception);
            } catch (NotSupportedException exception) {
                throw new ArgumentException("XShell Temp.Path must be a valid physical storage path.", nameof(config), exception);
            }
            if (config.Temp.FileSizeLimit <= 0) throw new ArgumentOutOfRangeException(nameof(config.Temp.FileSizeLimit), "XShell Temp.FileSizeLimit must be greater than zero.");
            if (config.Temp.ExpirationTime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(config.Temp.ExpirationTime), "XShell Temp.ExpirationTime must be greater than zero.");

            // validate independent host request paths without changing their values
            if (!IsRequestBasePath(config.App.BasePath, true)) throw new ArgumentException("XShell App.BasePath must be empty or a rooted request path starting with '/'.", nameof(config));
            if (!IsRequestBasePath(config.Resources.BasePath, true)) throw new ArgumentException("XShell Resources.BasePath must be empty or a rooted request path starting with '/'.", nameof(config));
            if (!IsRequestBasePath(config.XShell.BasePath, false)) throw new ArgumentException("XShell XShell.BasePath must be a non-empty rooted request path starting with '/'.", nameof(config));
            if (!IsRequestBasePath(config.Temp.BasePath, false)) throw new ArgumentException("XShell Temp.BasePath must be a non-empty rooted request path starting with '/'.", nameof(config));

            // validate values consumed by generated HTML and SPA fallback
            if (config.Server.UnhandledPrefixes == null) throw new ArgumentNullException(nameof(config.Server.UnhandledPrefixes), "XShell Server.UnhandledPrefixes must not be null.");
            for (var index = 0; index < config.Server.UnhandledPrefixes.Length; index++) {
                if (!IsRequestBasePath(config.Server.UnhandledPrefixes[index], false, true)) {
                    throw new ArgumentException($"XShell Server.UnhandledPrefixes[{index}] must be a non-empty rooted request path starting with '/'.", nameof(config));
                }
            }
        }
        private static bool IsRequestBasePath(string? path, bool allowEmpty, bool allowTrailingSlash = false) {
            if (path == "" && allowEmpty) return true;
            return !string.IsNullOrEmpty(path) && path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) && (allowTrailingSlash || !path.EndsWith('/')) &&
                path.IndexOfAny(['?', '#', '\\']) < 0 && !path.Any(char.IsWhiteSpace);
        }

    }
}
