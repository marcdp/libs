using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Factories.Tests {
    
    public class Base {

        //vars
        protected readonly IFactoryByUrl<DProjects.Factories.Tests.FactoryByUrlTests.ISomething> mFactoryByUrl;
        protected readonly IFactoryByUrlAndArgument<DProjects.Factories.Tests.FactoryByUrlAndArgumentTests.ISomething, string> mFactoryByUrlAndArgument;

        //constructor
        public Base() {
            //factory by url
            var services = new ServiceCollection();
            //factory by url 
            services.AddFactoryByUrl<FactoryByUrlTests.ISomething>(cfg => {
                cfg.AddFactoriesFromAssembly(typeof(FactoryByUrlTests).Assembly);
                cfg.AddAlias("111", "something1");
                cfg.AddAlias("222", "something2");
                cfg.AddAlias("333", "dir");
            });
            services.AddSingleton<FactoryByUrlTests.ISomething>(new FactoryByUrlTests.SomethingDefault());
            //factory by url  and argument
            services.AddFactoryByUrlAndArgument<FactoryByUrlAndArgumentTests.ISomething, string>(cfg => {
                cfg.AddFactoriesFromAssembly(typeof(FactoryByUrlAndArgumentTests).Assembly);
                cfg.AddAlias("111", "something1");
                cfg.AddAlias("222", "something2");
                cfg.AddAlias("333", "dir");
            });
            services.AddSingleton<FactoryByUrlAndArgumentTests.ISomething>(new FactoryByUrlAndArgumentTests.SomethingDefault());
            //password
            services.AddSingleton<IFactoryPasswordFiller>(new FactoryByUrlTests.FactoryPasswordFiller());
            //provider
            var provider = services.BuildServiceProvider();
            //get
            mFactoryByUrl = provider.GetRequiredService<IFactoryByUrl<FactoryByUrlTests.ISomething>>();
            mFactoryByUrlAndArgument = provider.GetRequiredService<IFactoryByUrlAndArgument<FactoryByUrlAndArgumentTests.ISomething, string>>();
        }
    }


}
