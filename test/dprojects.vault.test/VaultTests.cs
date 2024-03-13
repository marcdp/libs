using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace DProjects.Vault.Tests {

    public abstract class VaultTests<T> where T : IVault {

        //vars
        private readonly IVault mVault;


        //constructor 
        public VaultTests(string url) {
            //register connection factory
            var services = new ServiceCollection();
            services.AddFactoryByUrl<IVault>(cfg => {
                cfg.AddFactoriesFromAssembly(typeof(T).Assembly);    
            });
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IFactoryByUrl<IVault>>();
            mVault = factory.Create(url);
        }

        //tests
        [Fact]
        public async Task Open_ShouldOpenConnection() {
            // ...
             
        }

    }
}
