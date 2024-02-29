using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DProjects.Factories {


    public class FactoryByUrlAndArgumentConfiguration<TType,TArgument>() where TType : class {
        //vars
        internal List<FactoryByUrlAlias> Aliases = new();
        internal List<FactoryByUrlAndArgumentProtocol<TType, TArgument>> Protocols = new();
        //methods
        public void AddFactory(Type type) {
            if (!typeof(IFactoryByUrlAndArgument<TType, TArgument>).IsAssignableFrom(type)) throw new ArgumentException("Unable to register factory type. Type does not implements IFactoryByUrlAndProtocol<TType,TArgument>: " + type.FullName);
            var protocol = new FactoryByUrlAndArgumentProtocol<TType,TArgument>(type);
            Protocols.Add(protocol);
        }
        public void AddFactory<TFactory>() where TFactory : IFactoryByUrlAndArgument<TType, TArgument> {
            AddFactory(typeof(TFactory));
        }
        public void AddFactoriesFromAssembly<TAssembly>() where TAssembly : IAssembly { 
            AddFactoriesFromAssembly(typeof(TAssembly).Assembly);
        }
        public void AddFactoriesFromAssembly(System.Reflection.Assembly assembly) {
            foreach (var type in assembly.GetTypes().Where(x => typeof(IFactoryByUrlAndArgument<TType,TArgument>).IsAssignableFrom(x))) {
                AddFactory(type);
            }
        }
        public void AddAlias(string alias, string url, ServiceLifetime lifeTime = ServiceLifetime.Scoped) {
            Aliases.Add(new FactoryByUrlAlias(alias, url, lifeTime));            
        }
    }


}