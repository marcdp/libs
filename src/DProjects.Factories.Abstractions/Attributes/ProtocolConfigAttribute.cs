using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProtocolConfigAttribute : Attribute {

        // props
        public Type Type { get; }
        
        // ctor 
        public ProtocolConfigAttribute(Type type) {
            Type = type;
        }
    }


}