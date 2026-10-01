
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

using DProjects.Utils;

namespace DProjects.XShell {

    public static class Extensions {


        // inner class
        public class Configuration {
            public string AppBasePath { get; init; } = "";
            public string AppConfigPath { get; init; }  = ""; 
            public Dictionary<string,string> AppParams { get; init; } = new();
            public string ResourcesBase { get; init; } = "";
            public string[] UnhandledPrefixes { get; init; } = new string[] {"/_", "/api", "/temp"};
            public string TempPath { get; init; } = Path.Combine(Path.GetTempPath(), ResourceName, "temp");
            public string TempUrl { get; init; } = "/temp";
            public TimeSpan TempExpirationTime { get; init; } = TimeSpan.FromHours(1);
            public string CSPValue { get; init; } = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; ";
            public string XShellBasePath { get; init; } = "/_resources/DProjects.XShell/xshell";
        }


        // constants
        public const string ResourceName = "DProjects.XShell";
        public const string RequestPath = "/_resources/DProjects.XShell";
        //public const string ServiceWorkerRequestPath = "/_resources/DProjects.XShell/xshell/sw.js";


        // methods
        public static void AddXShell(this IServiceCollection services) {
            // register XShell services here when needed
        }
        public static void UseXShell(this WebApplication app, Configuration config) {

            // config webapplication
            var assembly = typeof(Extensions).Assembly;
            var environment = app.Environment.EnvironmentName;
            var isDevelopment = app.Environment.IsDevelopment();
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
            app.UseMiddleware<Middlewares.ResourcesMiddleware>(resourcePath, config.ResourcesBase + RequestPath, isDevelopment);

            // /_temp
            app.UseMiddleware<Middlewares.TempMiddleware>(config.TempPath, config.TempUrl, config.TempExpirationTime);

            // redirect canonical base URL
            app.Use(async (context, next) => {
                if (context.Request.Path == config.AppBasePath) {
                    context.Response.Redirect(config.AppBasePath + "/");
                    return;
                } else if (config.AppBasePath.Length > 0 && !context.Request.Path.StartsWithSegments(config.AppBasePath)) {
                    if (config.ResourcesBase.Length > 0 && context.Request.Path.StartsWithSegments(config.ResourcesBase)) {

                    } else {
                        context.Response.Redirect(config.AppBasePath + "/");
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
                    if (context.Request.Path == config.AppBasePath + aa) {
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
                if (!context.Request.Path.StartsWithSegments(config.AppBasePath, out var remaining)) {
                    await next();
                    return;
                }

                var slug = remaining.Value ?? "";

                // reserved prefixes must continue through the pipeline
                foreach (var unhandledPrefix in config.UnhandledPrefixes) {
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