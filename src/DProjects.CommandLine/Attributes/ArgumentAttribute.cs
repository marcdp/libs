using System;


namespace DProjects.CommandLine.Attributes {


    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ArgumentAttribute : Attribute {

        //variables
        public int Index { get; }
        public string Description { get; }
        public object? Default { get; }
        public string? Alias { get; }


        //constructors
        public ArgumentAttribute(int index, string description, object? aDefault, string? alias = null) {
            Index = index;
            Description = description;
            Default = aDefault;
            Alias = alias;
        }


    }

}

