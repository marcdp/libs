using System;

namespace DProjects.Commands.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class HistoryAttribute : Attribute {

        //variables
        public bool Value { get; }

        //constructors
        public HistoryAttribute(bool value) {
            this.Value = value;
        }

    }

}

