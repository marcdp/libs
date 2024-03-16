using DProjects.Factories.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace DProjects.Factories {


    public class FactoryByUrlConfiguration<T>() where T : class {

        //vars
        internal List<FactoryByUrlAlias> Aliases { get; } = new();
        internal List<FactoryByUrlProtocol<T>> Protocols { get; } = new();

        //methods
        public void AddFactory(Type type) {
            if (!typeof(IFactoryByUrl<T>).IsAssignableFrom(type)) throw new ArgumentException("Unable to register factory type. Type does not implements IFactoryByUrl<T>: " + type.FullName);
            var protocol = new FactoryByUrlProtocol<T>(type);
            Protocols.Add(protocol);
        } 
        public void AddFactory<TFactory>() where TFactory : IFactoryByUrl<T> {
            AddFactory(typeof(TFactory));
        }
        public void AddFactoriesFromAssembly<TAssembly>() where TAssembly : IAssembly { 
            AddFactoriesFromAssembly(typeof(TAssembly).Assembly);
        }
        public void AddFactoriesFromAssembly(System.Reflection.Assembly assembly) {
            foreach (var type in assembly.GetTypes().Where(x => typeof(IFactoryByUrl<T>).IsAssignableFrom(x))) {
                AddFactory(type);
            }
        }
        public void AddAlias(string alias, string url, ServiceLifetime lifeTime = ServiceLifetime.Scoped, string description = "") {
            Aliases.Add(new FactoryByUrlAlias(alias, description, url, lifeTime));            
        } 
        
    }


}