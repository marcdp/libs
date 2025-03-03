using System;

namespace DProjects.Commands.Attributes {


    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class BodyAttribute : Attribute {


        //properties
        public string Description { get; }


        //constructor
        public BodyAttribute(string description) {
            Description = description;
        }

    }

}

