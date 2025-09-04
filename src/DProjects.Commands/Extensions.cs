
using System;

using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Commands {

    public static class Extensions {

        public static void AddCommandsManager(this IServiceCollection services, Action<Configuration>? configurationHandler = null) {
            // Add singleton Configuration to the services
            var configuration = new Configuration(services, System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
            if (configurationHandler != null) {
                configurationHandler(configuration);
            }
            services.AddSingleton(configuration);
            // Add transient Manager to the services
            services.AddTransient<CommandsManager>();

            services.AddScoped<IEnvironment>(sp => {
                var configuration = sp.GetRequiredService<Configuration>();
                var commandsManager = sp.GetRequiredService<CommandsManager>();
                // Create a new environment with the current service provider and configuration
                return new Environment(commandsManager);
            });
        }
    }

}