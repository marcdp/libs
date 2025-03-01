
using System;

using Microsoft.Extensions.DependencyInjection;

namespace DProjects.CommandLine {
    public static class Extensions {

        public static void AddCommandLineManager(this IServiceCollection services, Action<Configuration>? configurationHandler = null) {
            // Add singleton CommandLineManagerConfiguration to the services
            var configuration = new Configuration(services, System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
            if (configurationHandler != null) {
                configurationHandler(configuration);
            }
            services.AddSingleton(configuration);
            // Add transient CommandLineManager to the services
            services.AddTransient<Manager>();
        }
    }

}