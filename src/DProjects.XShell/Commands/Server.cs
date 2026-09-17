using DProjects.Commands;
using DProjects.Commands.Attributes;
using Microsoft.AspNetCore.Builder;


namespace DProjects.XShell.Commands {

    [Description("Launch a web server")]
    public class Server() : ICommand {


        // Arguments
        [Flag('a', "App base url", "")]
        public string AppBase { get; init; } = "";
        [Flag('c', "App config file path", "")]
        public string AppConfig { get; init; } = "";
        [Flag('r', "Resource base url", "")]
        public string ResourceBase { get; init; } = "";


        //methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {

            // builder
            var builder = WebApplication.CreateBuilder();

            // add services
            builder.Services.AddXShell();

            // build app
            var app = builder.Build();

            // use XShell
            app.UseXShell(new Extensions.Configuration {
                AppBase = AppBase,
                AppConfig = (string.IsNullOrEmpty(AppConfig) ? ResourceBase + "/_resources/DProjects.XShell/samples/sample1/app.jsonc" : AppConfig),
                ResourcesBase = ResourceBase
            });

            // run app
            await app.RunAsync();

            // return
            return 0;
        }

    }

}
