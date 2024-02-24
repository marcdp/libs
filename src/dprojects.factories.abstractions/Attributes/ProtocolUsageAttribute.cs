using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProtocolUsageAttribute : Attribute {
        public string Usage { get; }
        public ProtocolUsageAttribute(string usage) {
            Usage = usage;
        }
    }


}