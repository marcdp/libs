
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace DProjects.XShell {

    public static class Extensions {


        // inner class
        public class Configuration {
            public string AppBasePath { get; init; } = "";
            public string AppConfigPath { get; init; }  = "";
            public Dictionary<string,string> AppParams { get; init; } = new();
            public string ResourcesBase { get; init; } = "";
            public string Favicon { get; init; } = "";
            public string[] UnhandledPrefixes { get; init; } = new string[] {"/_", "/api"};            
        }
        

        // constants
        public const string ResourceName = "DProjects.XShell";
        public const string RequestPath = "/_resources/DProjects.XShell";
        public const string ServiceWorkerRequestPath = "/_resources/DProjects.XShell/xshell/sw.js";


        // methods
        public static void AddXShell(this IServiceCollection services) {
            // register XShell services here when needed
        }
        public static void UseXShell(this WebApplication app, Configuration config) {

            // config webapplication
            var assembly = typeof(Extensions).Assembly;
            var environment = app.Environment.EnvironmentName;
            var isDevelopment = app.Environment.IsDevelopment();
            if (System.Diagnostics.Debugger.IsAttached) {
                environment = "Development";
                isDevelopment = true;
            }
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

            // register _resources
            app.UseMiddleware<Middlewares.ResourcesMiddleware>(
                resourcePath,
                config.ResourcesBase + RequestPath,
                isDevelopment);

            // map routes /
            var indexHtml = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        {(!string.IsNullOrEmpty(config.Favicon) ? "<link href=\"" + config.Favicon + "\" rel=\"icon\">" : "")}

                        <!-- config xshell -->
                        <meta name="xshell:app.basePath"    content="{config.AppBasePath}">
                        <meta name="xshell:app.configPath"  content="{config.AppConfigPath}">
                        <meta name="xshell:app.params"  content="{string.Join("&", config.AppParams.Select(kv => kv.Key + "=" + kv.Value))}">
                        <meta name="xshell:xshell.environment" content="{(environment)}">

                        <!-- bootstrap xshell -->
                        <script src="{config.ResourcesBase}/_resources/DProjects.XShell/xshell/bootstrap.js"></script>

                    </head>
                    <body>
                    </body>
                    </html>
                    """;
            var swJs = $"""
                    // import real service worker script from xshell cdn
                    importScripts("{config.ResourcesBase + ServiceWorkerRequestPath}"); 
                    """;

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

            // service worker
            app.MapGet(config.AppBasePath + "/sw.js", async context => {
                context.Response.ContentType = "text/javascript";
                await context.Response.WriteAsync(swJs);
            });

            // SPA fallback
            app.Use(async (context, next) => {

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
                await context.Response.WriteAsync(indexHtml);
            });


        }

    }
}