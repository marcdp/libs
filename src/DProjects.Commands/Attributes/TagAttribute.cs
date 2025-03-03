using System;

namespace DProjects.Commands.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class TagAttribute : Attribute {

        //variables
        public string Tag { get; }

        //constructors
        public TagAttribute(string tag) {
            this.Tag = tag;
        }

    }

}

