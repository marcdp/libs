using DProjects.Factories.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DProjects.Factories {


    // class
    public class FactoryByUrlProtocol<T> : IComparable<FactoryByUrlProtocol<T>> {

        //variables
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public string[] Examples { get; }
        public Type Factory { get; }


        //constructor
        public FactoryByUrlProtocol(Type factory, string name, string description, string usage, string[] examples) {
            Name = name;
            Description = description;
            Usage = usage;
            Examples = examples;
            Factory = factory;
        } 
        public FactoryByUrlProtocol(Type factory) {
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
        public int CompareTo(FactoryByUrlProtocol<T> other) {
            return Name.CompareTo(other.Name);
        }
        public override string ToString() {
            return Name + ":";
        }
    }
}