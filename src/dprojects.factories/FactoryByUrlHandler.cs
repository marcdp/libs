using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Factories {

    public class FactoryByUrlHandler<T>(string name, string description, Func<IServiceProvider, object?, T> handler, ServiceLifetime lifeTime) : IComparable<FactoryByUrlAlias>  {

        //props
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public Func<IServiceProvider, object?, T> Handler { get; set; } = handler;
        public ServiceLifetime Lifetime { get; set; } = lifeTime;


        //methods
        public int CompareTo(FactoryByUrlAlias other) {
            return Name.CompareTo(other.Name);
        }
        public override string ToString() {
            return Name;
        }
    }


}