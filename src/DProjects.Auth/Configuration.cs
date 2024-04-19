using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Auth {

    public static class Extensions {

        public static IServiceCollection AddAuthProviders(this IServiceCollection services, Action<Configuration> configuration) {
            var config = new Configuration(services);
            configuration.Invoke(config);
            return services;
        }

    }

    public class Configuration(IServiceCollection services) {

        public void AddAuthProvider<T>() where T : IAuthProvider {
            services.AddSingleton(typeof(IAuthProvider), typeof(T));
        }
    }



}