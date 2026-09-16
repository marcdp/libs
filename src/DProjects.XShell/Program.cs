using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DProjects.XShell {

    public static class Program {

        public static async Task<int> Main(string[] args) {

            // builder
            var builder = WebApplication.CreateBuilder(args);

            // add services
            builder.Services.AddXShell();

            // build app
            var app = builder.Build();

            // default app to server
            var appBase = "/prefix";
            var appConfig = (args.Length > 0) ? args[0] : appBase + Extensions.RequestPath + "/apps/app1/app.jsonc";
            var unhandledPrefixes = new string[] { "/api", "/_" };

            // use XShell
            app.UseXShell(appBase, appConfig, unhandledPrefixes);

            // run app
            await app.RunAsync();

            // return
            return 0;
        }
    }
}
