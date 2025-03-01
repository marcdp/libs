using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CategoryAttribute : Attribute {

        //variables
        public string Category { get; }

        //constructors
        public CategoryAttribute(string category) {
            this.Category = category;
        }

    }

}

