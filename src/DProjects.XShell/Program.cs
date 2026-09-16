using Microsoft.AspNetCore.Builder;

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
            app.UseXShell();

            // run app
            await app.RunAsync();

            // return
            return 0;
        }
    }
}
