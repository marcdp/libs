using System;

namespace DProjects.CommandLine.Attributes {

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class AssemblyAttribute : Attribute {

        //constructors
        public AssemblyAttribute(Type handler) {
            Handler = handler;
        }

        //properties
        public Type Handler { get; private set; }



    }

}

