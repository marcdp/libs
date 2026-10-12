using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.XShell.Services;
using Microsoft.AspNetCore.Builder;


namespace DProjects.XShell.Commands {

    [Description("Launch a web server")]
    [Example("DProjects.XShell server --app-config-path /_resources/DProjects.XShell/x-demo/module.jsonc --param a=123 --param b=456", "")]
    public class Server() : ICommand {


        // Arguments
        [Flag('a', "App base path", "")]
        public string AppBasePath { get; init; } = "";
        [Flag('c', "App config file path", "")]
        public string AppConfigPath { get; init; } = "";
        [Flag('e', "Environment", "")]
        public string Environment { get; init; } = "";
        [Flag('p', "Parameter", "")]
        public string[] Param { get; init; } = [];
        [Flag('r', "Resources name", "DProjects.XShell")]
        public string ResourcesName { get; init; } = "DProjects.XShell";


        //methods
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {

            // validate selected bundled resources before starting the host
            ResourceNameValidator.Validate(ResourcesName);

            // builder
            var builder = WebApplication.CreateBuilder();

            // build app
            var app = builder.Build();

            // use XShell
            app.UseXShell(BuildConfiguration());

            // run app
            await app.RunAsync();

            // return
            return 0;
        }
        internal Extensions.Configuration BuildConfiguration() {

            // derive every resource route from the same selected resource-set name
            return new Extensions.Configuration {
                App = new Extensions.AppConfig {
                    BasePath = AppBasePath,
                    ConfigPath = string.IsNullOrEmpty(AppConfigPath) ? AppBasePath + "/_resources/" + ResourcesName + "/x-demo/module.jsonc" : AppConfigPath,
                    Params = Param.ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1])
                },
                Temp = new Extensions.TempConfig {
                    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), ResourcesName, "temp"),
                    BasePath = AppBasePath + "/temp",
                    ExpirationTime = TimeSpan.FromHours(1)
                },
                Resources = new Extensions.ResourcesConfig {
                    BasePath = AppBasePath + "/_resources/" + ResourcesName,
                    ResourceName = ResourcesName
                },
                XShell = new Extensions.XShellConfig {
                    BasePath = AppBasePath + "/_resources/" + ResourcesName + "/xshell"
                },
                Server = new Extensions.ServerConfig {
                    Environment = Environment
                }
            };
        }

    }

}
