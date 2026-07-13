using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class MaximumAttribute : Attribute {
        public double Max { get; }
        public bool Inclusive { get; }
        public MaximumAttribute(double max, bool inclusive = true) {
            Max = max;
            Inclusive = inclusive;
        }
    }


}