using System;

namespace DProjects.Factories.Attributes {


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProtocolHelp : Attribute {
        public string Help { get; }
        public ProtocolHelp(string help) {
            Help = help;
        }
    }


}