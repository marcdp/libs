using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class ExampleAttribute : Attribute {
        public string Value { get; }
        public string Description { get; }
        public ExampleAttribute(string value, string description = "") {
            Value = value;
            Description = description;
        }
    }


}