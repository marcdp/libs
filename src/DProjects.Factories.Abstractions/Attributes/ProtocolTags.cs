using System;

namespace DProjects.Factories.Attributes {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ProtocolTags : Attribute {

        //props
        public string[] Tags { get; }

        //constructor
        public ProtocolTags(params string[] tags) {
            Tags = tags;
        }

    }

}