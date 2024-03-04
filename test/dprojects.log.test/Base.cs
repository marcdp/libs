using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Log.Tests {
    public class Base {
        protected readonly FactoryByUrl<ILogEntrySerializer> mLogEntrySerializerFactoryByUrl;
        public Base() {
            var services = new ServiceCollection();
            services.AddFactoryByUrl<ILogEntrySerializer>(cfg => {
                cfg.AddFactoriesFromAssembly<DProjects.Log.Assembly>();
            });
            var serviceProvider = services.BuildServiceProvider();
            mLogEntrySerializerFactoryByUrl = serviceProvider.GetRequiredService<FactoryByUrl<ILogEntrySerializer>>()!;
        }
    }
}
