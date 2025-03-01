using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ExampleAttribute : Attribute {

        //variables
        public string Example { get; }
        public string Description { get; }

        //constructors
        public ExampleAttribute(string example, string description) {
            this.Example = example;
            this.Description = description;
        }

    }

}

