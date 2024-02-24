using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace DProjects.Factories {

    public static class Extensions {

        // Add 
        public static IServiceCollection AddFactoryByUrl<T>(this IServiceCollection services, Action<FactoryByUrlConfiguration<T>> configuration) where T : class {
            var config = new FactoryByUrlConfiguration<T>();
            configuration.Invoke(config);
            config.Protocols.Sort();
            config.Aliases.Sort();
            //add factory instance
            services.AddTransient(typeof(IFactoryByUrl<T>), (services) => {
                return new FactoryByUrl<T>(config, services);
            });
            services.AddTransient(typeof(FactoryByUrl<T>), (services) => {
                return services.GetRequiredService<IFactoryByUrl<T>>();
            });
            //add alias
            foreach (var alias in config.Aliases) {
                if (alias.Lifetime == ServiceLifetime.Scoped) {
                    services.AddKeyedScoped<T>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<T>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Transient) {
                    services.AddKeyedTransient<T>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<T>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Singleton) {
                    services.AddKeyedSingleton<T>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<T>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                }
            }
            //protocols
            foreach (var protocol in config.Protocols) {
                services.AddKeyedTransient(typeof(IFactoryByUrl<T>), protocol.Name, protocol.Factory);
            }
            //return
            return services;
        }

    }


}