using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Log.Provider {

    public class LoggerProviderConfiguration(IServiceCollection services)  {

        //props
        public IServiceCollection Services { get; } = services;
        public string Url { get; set; } = "stdout:?format=json";

    }

}

