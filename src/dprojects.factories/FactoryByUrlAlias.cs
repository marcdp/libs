using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Factories {

    public class FactoryByUrlAlias(string name, string value, ServiceLifetime lifeTime) {

        public string Name { get; set; } = name;
        public string Value { get; set; } = value;
        public ServiceLifetime Lifetime { get; set; } = lifeTime;

    }



}