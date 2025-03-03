using System;

namespace DProjects.Commands.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class SynopsisAttribute : Attribute {

        //variables
        public string Example { get; }
        public string Description { get; }

        //constructors
        public SynopsisAttribute(string example) {
            this.Example = example;
            this.Description = "";
        }
        public SynopsisAttribute(string example, string description) {
            this.Example = example;
            this.Description = description;
        }

    }

}

