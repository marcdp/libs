using System;

namespace DProjects.Factories.Attributes {


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProtocolHelpAttribute : Attribute {
        public string Help { get; }
        public ProtocolHelpAttribute(string help) {
            Help = help;
        }
    }


}