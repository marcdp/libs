using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProtocolExampleAttribute : Attribute {
        public string Example { get; }
        public string Description { get; }
        public ProtocolExampleAttribute(string example, string description) {
            Example = example;
            Description = description;
        }
    }


}