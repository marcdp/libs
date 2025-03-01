using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class NameAttribute : Attribute {

        //variables
        public string Name { get; }

        //constructors
        public NameAttribute(string name) {
            this.Name = name;

        }

    }

}

