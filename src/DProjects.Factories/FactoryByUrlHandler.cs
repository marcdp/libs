using Microsoft.Extensions.DependencyInjection;
using System;

namespace DProjects.Factories {

    public class FactoryByUrlHandler<TType>(string name, string description, string usage, string[] examples, Func<IServiceProvider, object?, TType> handler, ServiceLifetime lifeTime) : IComparable<FactoryByUrlAlias>  where TType  : class{

        //props
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public string Usage { get; set; } = usage;
        public string[] Examples { get; set; } = examples;
        public Func<IServiceProvider, object?, TType> Handler { get; set; } = handler;
        public ServiceLifetime Lifetime { get; set; } = lifeTime;


        //methods
        public int CompareTo(FactoryByUrlAlias other) {
            return Name.CompareTo(other.Name);
        }
        public override string ToString() {
            return Name;
        }
    }

    public class FactoryByUrlHandler<TType, TArgument>(string name, string description, string usage, string[] examples, Func<IServiceProvider, object?, TArgument, TType> handler, ServiceLifetime lifeTime) : IComparable<FactoryByUrlAlias> where TType : class where TArgument : class{

        //props
        public string Name { get; set; } = name;
        public string Description { get; set; } = description;
        public string Usage { get; set; } = usage;
        public string[] Examples { get; set; } = examples;
        public Func<IServiceProvider, object?, TArgument, TType> Handler { get; set; } = handler; 
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