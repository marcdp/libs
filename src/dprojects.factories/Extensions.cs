using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace DProjects.Factories {

    public static partial class Extensions {

        // Add 
        public static IServiceCollection AddFactoryByUrl<TType>(this IServiceCollection services, Action<FactoryByUrlConfiguration<TType>> configuration) where TType : class {
            //config
            var config = new FactoryByUrlConfiguration<TType>();
            configuration.Invoke(config);
            //add dependency-injection factory (ex: dependency-injection:keyed-service1, dependency-injection:keyed-service2, ...)
            // Meaby we should activate generic injection : ????
            //config.AddFactory<DependencyInjectionFactory<TType>>();   
            //sort
            config.Protocols.Sort();
            config.Aliases.Sort();
            //add factory instance
            services.AddTransient(typeof(IFactoryByUrl<TType>), (services) => {
                return new FactoryByUrl<TType>(config, services);
            });
            services.AddTransient(typeof(FactoryByUrl<TType>), (services) => {
                return services.GetRequiredService<IFactoryByUrl<TType>>();
            });
            //add alias
            foreach (var alias in config.Aliases) {
                if (alias.Lifetime == ServiceLifetime.Scoped) {
                    services.AddKeyedScoped<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Transient) {
                    services.AddKeyedTransient<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Singleton) {
                    services.AddKeyedSingleton<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                }
            }
            //protocols
            foreach (var protocol in config.Protocols) {
                services.AddKeyedTransient(typeof(IFactoryByUrl<TType>), protocol.Name, protocol.Factory);
            }
            //return
            return services;
        }
        public static IServiceCollection AddFactoryByUrlAndArgument<TType, TArgument>(this IServiceCollection services, Action<FactoryByUrlAndArgumentConfiguration<TType, TArgument>> configuration) where TType : class {
            //config
            var config = new FactoryByUrlAndArgumentConfiguration<TType, TArgument>();
            configuration.Invoke(config);
            //sort
            config.Protocols.Sort();
            config.Aliases.Sort();
            //add factory instance
            services.AddTransient(typeof(IFactoryByUrlAndArgument<TType, TArgument>), (services) => {
                return new FactoryByUrlAndArgument<TType, TArgument>(config, services);
            });
            services.AddTransient(typeof(FactoryByUrlAndArgument<TType, TArgument>), (services) => {
                return services.GetRequiredService<IFactoryByUrlAndArgument<TType, TArgument>>();
            });
            //add alias
            foreach (var alias in config.Aliases) {
                if (alias.Lifetime == ServiceLifetime.Scoped) {
                    services.AddKeyedScoped<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Transient) {
                    services.AddKeyedTransient<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                } else if (alias.Lifetime == ServiceLifetime.Singleton) {
                    services.AddKeyedSingleton<TType>(alias.Name, (services, key) => {
                        var factory = services.GetRequiredService<IFactoryByUrl<TType>>();
                        return factory.Create(key?.ToString() ?? "");
                    });
                }
            }
            //protocols
            foreach (var protocol in config.Protocols) {
                services.AddKeyedTransient(typeof(IFactoryByUrlAndArgument<TType,TArgument>), protocol.Name, protocol.Factory);
            }
            //return
            return services;
        }

    }


}