using Microsoft.Extensions.Hosting;
using DProjects.Log;
using DProjects.Log.Provider;
using DProjects.Factories;
using Microsoft.Extensions.Logging;


namespace Sample1 {

    internal class Program {

        static async Task Main(string[] args) {

            // Create HostAppBuilder
            var hostAppBuilder = Host.CreateApplicationBuilder(args);
            hostAppBuilder.Logging.ClearProviders();
            hostAppBuilder.Logging.AddLogProvider((cfg) => {
                //register ILog factory
                cfg.Services.AddFactoryByUrl<ILog>(cfg => {
                    cfg.AddFactoriesFromAssembly<DProjects.Log.Assembly>();
                });
                //register ILogFormatter factory
                cfg.Services.AddFactoryByUrl<ILogEntrySerializer>(cfg => {
                    cfg.AddFactoriesFromAssembly<DProjects.Log.Assembly>();
                });
                //stdout
                //cfg.Url = "stdout:?format=json";
                //cfg.Url = "stdout:?format=rat";
            });

            hostAppBuilder.Services.AddFactoryByUrl<DProjects.Queues.IQueue>(cfg => {
                cfg.AddFactoriesFromAssembly<DProjects.Queues.Assembly>();
            });

            // Build Host
            var host = hostAppBuilder.Build();
            
            // Run
            await host.RunAsync();

        }
    }
}
