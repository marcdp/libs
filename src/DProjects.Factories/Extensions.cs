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
            // add handlers
            foreach (var handler in config.Handlers) {
                if (handler.Lifetime == ServiceLifetime.Scoped) {
                    services.AddKeyedScoped<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
                    });
                } else if (handler.Lifetime == ServiceLifetime.Transient) {
                    services.AddKeyedTransient<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
                    });
                } else if (handler.Lifetime == ServiceLifetime.Singleton) {
                    services.AddKeyedSingleton<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
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
        public static IServiceCollection AddFactoryByUrl<TType, TArgument>(this IServiceCollection services, Action<FactoryByUrlConfiguration<TType, TArgument>> configuration) where TType : class where TArgument : class  {
            //config
            var config = new FactoryByUrlConfiguration<TType, TArgument>();
            configuration.Invoke(config);
            //add dependency-injection factory (ex: dependency-injection:keyed-service1, dependency-injection:keyed-service2, ...)
            // Meaby we should activate generic injection : ????
            //config.AddFactory<DependencyInjectionFactory<TType>>();   
            //sort
            config.Protocols.Sort();
            config.Aliases.Sort();
            //add factory instance
            services.AddTransient(typeof(IFactoryByUrl<TType, TArgument>), (services) => {
                return new FactoryByUrl<TType, TArgument>(config, services);
            });
            services.AddTransient(typeof(FactoryByUrl<TType, TArgument>), (services) => {
                return services.GetRequiredService<IFactoryByUrl<TType, TArgument>>();
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
            // add handlers
            foreach (var handler in config.Handlers) {
                if (handler.Lifetime == ServiceLifetime.Scoped) {
                    services.AddKeyedScoped<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
                    });
                } else if (handler.Lifetime == ServiceLifetime.Transient) {
                    services.AddKeyedTransient<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
                    });
                } else if (handler.Lifetime == ServiceLifetime.Singleton) {
                    services.AddKeyedSingleton<TType>(handler.Name, (services, key) => {
                        return handler.Handler(services, key);
                    });
                }
            }
            //protocols
            foreach (var protocol in config.Protocols) {
                services.AddKeyedTransient(typeof(IFactoryByUrl<TType, TArgument>), protocol.Name, protocol.Factory);
            }
            //return
            return services;
        }

    }


}