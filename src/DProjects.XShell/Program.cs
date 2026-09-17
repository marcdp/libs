using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DProjects.Commands;

namespace DProjects.XShell {

    public static class Program {

        public static async Task<int> Main(string[] args) {
             
            // create host builder
            var builder = Host.CreateApplicationBuilder(args);

            // x3 home
            var x3HomePath = System.Environment.GetEnvironmentVariable("X3_HOME");
            if (string.IsNullOrWhiteSpace(x3HomePath)) {
                x3HomePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".x3");
            }

            // commands manager
            builder.Services.AddCommandsManager(cfg => {
                // add commands
                cfg.AddCommandsFromAssembly(DProjects.XShell.Assembly.Instance);
            });

            //  Build App 
            var app = builder.Build();

            // Execute
            var commandManager = app.Services.GetRequiredService<CommandsManager>();
            int result = await commandManager.ExecuteAsync(args, CancellationToken.None);

            // Return
            return result;

        }
    }
}
