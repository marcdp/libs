
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

    }
}
