using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DProjects.Log.Provider {

    public static class Configuration {

        public static ILoggingBuilder AddLogProvider(this ILoggingBuilder builder, Action<LoggerProviderConfiguration> action) {
            //add provider config
            var configuration = new LoggerProviderConfiguration(builder.Services);
            builder.Services.AddSingleton(configuration);
            action(configuration);
            // add provider
            builder.Services.AddSingleton<ILoggerProvider, LoggerProvider>();
            return builder;
        }
    }


}

