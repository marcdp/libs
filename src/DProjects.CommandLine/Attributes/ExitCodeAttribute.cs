using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ExitCodeAttribute : Attribute {

        //variables
        public int Code { get; }
        public string Description { get; }

        //constructors
        public ExitCodeAttribute(int code, string description) {
            this.Code = code;
            this.Description = description;
        }

    }

}

