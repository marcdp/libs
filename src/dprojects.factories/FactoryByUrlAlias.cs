using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Factories {

    public class FactoryByUrlAlias(string name, string description, string value, ServiceLifetime lifeTime) : IComparable<FactoryByUrlAlias>  {

        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public string Value { get; set; } = value;
        public ServiceLifetime Lifetime { get; set; } = lifeTime;

        public int CompareTo(FactoryByUrlAlias other) {
            return Name.CompareTo(other.Name);
        }
    }



}