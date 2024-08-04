using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Secrets {

    public static class Extensions {

        // Configuration class
        public class Configuration(IServiceCollection services) {
        }

        // Configuration methods
        public static IServiceCollection AddAuthProviders(this IServiceCollection services, Action<Configuration> configuration) {
            var config = new Configuration(services);
            configuration.Invoke(config);
            return services;
        }

    }




}