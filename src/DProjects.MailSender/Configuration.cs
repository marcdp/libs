using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.MailSender {

    public static class Extensions {

        public static IServiceCollection AddAuthProviders(this IServiceCollection services, Action<Configuration> configuration) {
            var config = new Configuration(services);
            configuration.Invoke(config);
            return services;
        }

    }

    public class Configuration {
        private readonly IServiceCollection mServices;

        public Configuration(IServiceCollection services) {
            this.mServices = services ?? throw new ArgumentNullException(nameof(services));
        }

        //public void AddSeAuthProvider<T>() where T : IAuthProvider {
        //    mServices.AddSingleton(typeof(IAuthProvider), typeof(T));
        //}
    }



}