using DProjects.Commands;
using DProjects.Commands.Attributes;
using Microsoft.AspNetCore.Builder;


namespace DProjects.XShell.Commands {

    [Description("Launch a web server")]
    [Example("DProjects.XShell server --app-config /_resources/DProjects.XShell/modules/test/module.jsonc --param a=123 --param b=456", "")]
    public class Server() : ICommand {


        // Arguments
        [Flag('a', "App base url", "")]
        public string AppBase { get; init; } = "";
        [Flag('c', "App config file path", "")]
        public string AppConfig { get; init; } = "";
        [Flag('r', "Resource base url", "")]
        public string ResourceBase { get; init; } = "";
        [Flag('p', "Parameter", "")]
        public string[] Param { get; init; } = [];


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
                AppParams = Param.ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1]),
                ResourcesBase = ResourceBase
            });

            // run app
            await app.RunAsync();

            // return
            return 0;
        }

    }

}
