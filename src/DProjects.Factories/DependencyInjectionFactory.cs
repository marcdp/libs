using DProjects.Factories.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Factories {

    [Protocol("dependency-injection")]
    [ProtocolUsage("dependency-injection:KEY")]
    [ProtocolExample("dependency-injection:service-key1", "")]
    [ProtocolExample("dependency-injection:service-key2", "")]
    public class DependencyInjectionFactory<T>(IServiceProvider services) : IFactoryByUrl<T> where T : class {
        public T Create(string url) {
            var key = url.Substring(url.IndexOf(":") + 3).Replace("/","");   
            return services.GetRequiredKeyedService<T>(key);
        }
    }
     
}