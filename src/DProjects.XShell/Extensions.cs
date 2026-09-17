
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
            public string AppBase { get; init; } = "";
            public string AppConfig { get; init; }  = "";
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
            var isDevelopment = app.Environment.IsDevelopment() || System.Diagnostics.Debugger.IsAttached;
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

            // register content type provider for .jsonc files
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".jsonc"] = "application/json";

            // map /_resources/DProjects.XShell
            var fileProvider = new PhysicalFileProvider(resourcePath);
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = fileProvider,
                RequestPath = config.ResourcesBase + RequestPath,
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = context => {
                    if (isDevelopment) {
                        context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                        context.Context.Response.Headers.Pragma = "no-cache";
                        context.Context.Response.Headers.Expires = "0";
                    }
                }
            });

            // map routes /
            var indexHtml = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        <link href="{config.Favicon}" rel="icon">

                        <!-- config xshell -->
                        <meta name="xshell.app_base_url" content="{config.AppBase}">
                        <meta name="xshell.app_config_url" content="{config.AppConfig}">
                        <meta name="xshell.sw_url" content="{config.AppBase}/sw.js">

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
                if (context.Request.Path == config.AppBase) {
                    context.Response.Redirect(config.AppBase + "/");
                    return;
                } else if (config.AppBase.Length > 0 && !context.Request.Path.StartsWithSegments(config.AppBase)) {
                    if (config.ResourcesBase.Length > 0 && context.Request.Path.StartsWithSegments(config.ResourcesBase)) {

                    } else {
                        context.Response.Redirect(config.AppBase + "/");
                        return;

                    }
                }
                await next();
            });

            // select registered endpoints
            app.UseRouting();

            // service worker
            app.MapGet(config.AppBase + "/sw.js", async context => {
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

                // only handle requests inside appBase
                if (!context.Request.Path.StartsWithSegments(config.AppBase, out var remaining)) {
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