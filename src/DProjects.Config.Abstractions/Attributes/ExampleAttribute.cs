using System;

namespace DProjects.Config.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class ExampleAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute {

        // props
        public string Value { get; }
        public string Description { get; }

        // ctor
        public ExampleAttribute(string value, string description = "") {
            Value = value;
            Description = description;
        }
    }


}