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

            // use XShell
            var appBase = "";
            var resourceBase = "";
            app.UseXShell( new Extensions.Configuration {
                AppBase = appBase,
                AppConfig = (args.Length > 0) ? args[0] : resourceBase + Extensions.RequestPath + "/samples/sample1/app.jsonc",
                ResourcesBase = resourceBase
            });

            // run app
            await app.RunAsync();

            // return
            return 0;
        }
    }
}
