using System;

namespace DProjects.Commands.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class FlagAttribute : Attribute {

        //variables
        public char Char { get; }
        public string Description { get; }
        public object? Default { get; }
        public string? Alias { get; }

        //constructor
        public FlagAttribute(char aChar, string description, object? aDefault, string? alias = null) {
            Char = aChar;
            Description = description;
            Default = aDefault;
            Alias = alias;
        }

    }

}

