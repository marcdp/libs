using System;

namespace DProjects.Commands.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute {

        //variables
        public string Description { get; }

        //constructors
        public DescriptionAttribute(string description) {
            this.Description = description;
        }

    }

}

