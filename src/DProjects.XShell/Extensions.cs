
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DProjects.XShell {

    public static class Extensions {

        // constants
        public const string ResourceName = "DProjects.XShell";
        public const string RequestPath = "/_resources/DProjects.XShell";

        // methods
        public static void AddXShell(this IServiceCollection services) {
            // register XShell services here when needed
        }

        public static void UseXShell(this WebApplication app) {
            // config webapplication
            var assembly = typeof(Extensions).Assembly;
            var isDevelopment = app.Environment.IsDevelopment() || System.Diagnostics.Debugger.IsAttached;

            // detect resourcePath
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
            if (!Directory.Exists(resourcePath)) {
                throw new DirectoryNotFoundException($"XShell resources directory not found: {resourcePath}");
            }

            // use static files
            var fileProvider = new PhysicalFileProvider(resourcePath);
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = fileProvider,
                RequestPath = RequestPath,
                OnPrepareResponse = context => {
                    if (isDevelopment) {
                        context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                        context.Context.Response.Headers.Pragma = "no-cache";
                        context.Context.Response.Headers.Expires = "0";
                    }
                }
            });
        }
    }
}