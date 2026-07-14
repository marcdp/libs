using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class ProtocolConfigPropertyAttribute : Attribute {

        // props
        public string Description { get; }
        public string? Help { get; set; }
        
        // ctor 
        public ProtocolConfigPropertyAttribute(string description) {
            Description = description;
        }
    }


}