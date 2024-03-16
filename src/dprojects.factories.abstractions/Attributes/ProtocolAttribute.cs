using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProtocolAttribute : Attribute {

        //delegate
        public delegate T CreateDelegate<T>(string Url);

        //props
        public string Name { get; }
        public string Description { get; }

        //constructor
        public ProtocolAttribute(string name, string description = "") { 
            Name = name;
            Description = description;
        }

    }


}