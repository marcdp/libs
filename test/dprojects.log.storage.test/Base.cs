using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Log.Storage.Tests {

    public class Base {

        //vars
        protected readonly FactoryByUrl<ILogStorageEntryDeserializer> mLogStorageEntryDeserializerFactoryByUrl;
        protected readonly FactoryByUrl<ILogEntrySerializer> mLogEntrySerializerFactoryByUrl;

        //constructor
        public Base() {
            var services = new ServiceCollection();
            services.AddFactoryByUrl<ILogStorageEntryDeserializer>(cfg => {
                cfg.AddFactoriesFromAssembly<DProjects.Log.Storage.Assembly>();
            });
            services.AddFactoryByUrl<ILogEntrySerializer>(cfg => {
                cfg.AddFactoriesFromAssembly<DProjects.Log.Assembly>();
            });
            var serviceProvider = services.BuildServiceProvider();
            mLogStorageEntryDeserializerFactoryByUrl = serviceProvider.GetRequiredService<FactoryByUrl<ILogStorageEntryDeserializer>>()!;
            mLogEntrySerializerFactoryByUrl = serviceProvider.GetRequiredService<FactoryByUrl<ILogEntrySerializer>>()!;
        }
    }
}
