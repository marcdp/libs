using System;
using System.Collections;
using System.Collections.Generic;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ProtocolParameterAttribute : Attribute {

        // props
        public string Name { get; }
        public Type Type { get; }
        public string? Description { get; set; }
        public object? DefaultValue { get; set; }
        public bool Required { get; set; }
        public string? Help { get; set; }
        public IDictionary<string,string>? Examples { get; set; }
        //public ProtocolConfigPropertyValidationAttribute[]? Validations { get; set; }

        // ctor (optional parameter — has a default value)
        public ProtocolParameterAttribute(string name, Type type, string description, object? defaultValue = null) {
            Name = name;
            Type = type;
            Description = description;
            Required = false;
            DefaultValue = defaultValue;
            Required = defaultValue == null;
        }
    }

}