
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
        //public class Configuration {

        //}
        

        // constants
        public const string ResourceName = "DProjects.XShell";
        public const string RequestPath = "/_resources/DProjects.XShell";
        public const string ServiceWorkerRequestPath = "/_resources/DProjects.XShell/xshell/sw.js";


        // methods
        public static void AddXShell(this IServiceCollection services) {
            // register XShell services here when needed
        }
        public static void UseXShell(this WebApplication app, string appBase, string appConfig, string[] unhandledPrefixes) {

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
                RequestPath = appBase + RequestPath,
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

                        <!-- config meta tags for xshell -->
                        <meta name="xshell.app_base_url" content="{appBase}">
                        <meta name="xshell.app_config_url" content="{appConfig}">
                        <meta name="xshell.sw_url" content="{appBase}/sw.js">

                        <!-- bootstrap -->
                        <script src="{appBase}/_resources/DProjects.XShell/xshell/bootstrap.js"></script>

                    </head>
                    <body>
                    </body>
                    </html>
                    """;
            var swJs = $"""
                    // import real service worker script from xshell cdn
                    importScripts("{appBase + ServiceWorkerRequestPath}"); 
                    """;

            // redirect canonical base URL
            app.Use(async (context, next) => {
                if (context.Request.Path == appBase) {
                    context.Response.Redirect(appBase + "/");
                    return;
                } else if (appBase.Length > 0 && !context.Request.Path.StartsWithSegments(appBase)) {
                    context.Response.Redirect(appBase + "/");
                    return;
                }
                await next();
            });

            // select registered endpoints
            app.UseRouting();

            // service worker
            app.MapGet(appBase + "/sw.js", async context => {
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
                if (!context.Request.Path.StartsWithSegments(appBase, out var remaining)) {
                    await next();
                    return;
                }

                var slug = remaining.Value ?? "";

                // reserved prefixes must continue through the pipeline
                foreach (var unhandledPrefix in unhandledPrefixes) {
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