using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class HelpAttribute : Attribute {

        //variables
        public string Help { get; }

        //constructors
        public HelpAttribute(string help) {
            this.Help = help;
        }

    }

}

