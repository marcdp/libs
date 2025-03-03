using System;

namespace DProjects.Commands.Attributes {

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

