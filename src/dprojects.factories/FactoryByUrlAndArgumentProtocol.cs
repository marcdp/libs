using DProjects.Factories.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DProjects.Factories {


    // class
    public class FactoryByUrlAndArgumentProtocol<TType,TArgument> : IComparable<FactoryByUrlAndArgumentProtocol<TType, TArgument>> {

        //variables
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public string[] Examples { get; }
        public Type Factory { get; }


        //constructor
        public FactoryByUrlAndArgumentProtocol(Type factory, string name, string description, string usage, string[] examples) {
            Name = name;
            Description = description;
            Usage = usage;
            Examples = examples;
            Factory = factory;
        } 
        public FactoryByUrlAndArgumentProtocol(Type factory) {
            var protocolAttribute = factory.GetCustomAttribute<ProtocolAttribute>();
            Name = protocolAttribute.Name;
            Description = protocolAttribute.Description;
            var protocolUsageAttribute = factory.GetCustomAttribute<ProtocolUsageAttribute>();
            Usage = protocolUsageAttribute?.Usage ?? "";
            var examples = new List<string>();
            foreach (var protocolExampleAttribute in factory.GetCustomAttributes<ProtocolExampleAttribute>()) {
                examples.Add(protocolExampleAttribute.Example);
            }
            Examples = examples.ToArray();
            Factory = factory;
        }

        //methods
        public int CompareTo(FactoryByUrlAndArgumentProtocol<TType, TArgument> other) {
            return Name.CompareTo(other.Name);
        }
    }
}