using System;

namespace DProjects.Commands.Attributes {

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

