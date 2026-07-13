using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProtocolExampleAttribute : Attribute {
        public string Value { get; }
        public string Description { get; }
        public ProtocolExampleAttribute(string value, string description) {
            Value = value;
            Description = description;
        }
    }


}