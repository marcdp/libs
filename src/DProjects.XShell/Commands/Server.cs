using DProjects.Commands;
using DProjects.Commands.Attributes;
using Microsoft.AspNetCore.Builder;


namespace DProjects.XShell.Commands {

    [Description("Launch a web server")]
    [Example("DProjects.XShell server --app-config-path /_resources/DProjects.XShell/x-demo/module.jsonc --param a=123 --param b=456", "")]
    public class Server() : ICommand {


        // Arguments
        [Flag('a', "Base path", "")]
        public string AppBasePath { get; init; } = "";
        [Flag('c', "App config file path", "")]
        public string AppConfigPath { get; init; } = "";
        [Flag('e', "Environment", "")]
        public string Environment { get; init; } = "";
        [Flag('p', "Parameter", "")]
        public string[] Param { get; init; } = [];


        //methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {

            // builder
            var builder = WebApplication.CreateBuilder();

            // build app
            var app = builder.Build();

            // use XShell
            app.UseXShell(new Extensions.Configuration {
                App = new Extensions.AppConfig {
                    BasePath = AppBasePath,
                    ConfigPath = (string.IsNullOrEmpty(AppConfigPath) ? AppBasePath + "/_resources/DProjects.XShell/x-demo/module.jsonc" : AppConfigPath),
                    Params = Param.ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1])
                },
                Temp = new Extensions.TempConfig {
                    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Extensions.ResourceName, "temp"),
                    BasePath = AppBasePath + "/temp",
                    ExpirationTime = TimeSpan.FromMinutes(30),
                    FileSizeLimit = 10 * 1024 * 1024 // 10 MB
                },
                Resources = new Extensions.ResourcesConfig {
                    BasePath = AppBasePath + "/_resources/" + Extensions.ResourceName
                },
                XShell = new Extensions.XShellConfig {
                    BasePath = AppBasePath + "/_resources/" + Extensions.ResourceName + "/xshell"
                },
                Server = new Extensions.ServerConfig {
                    Environment = Environment
                }
            });

            // run app
            await app.RunAsync();

            // return
            return 0;
        }

    }

}
