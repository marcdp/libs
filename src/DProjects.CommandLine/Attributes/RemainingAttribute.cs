using System;

namespace DProjects.CommandLine.Attributes {


    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class RemainingAttribute : Attribute {


        //properties
        public string Description { get; }
        public string? Default { get; }
        public Boolean AvoidExpansions { get; }


        //constructor
        public RemainingAttribute(string description, string? aDefault, bool avoidExpansions) {
            this.Description = description;
            this.Default = aDefault;
            this.AvoidExpansions = avoidExpansions;
        }


    }

}

