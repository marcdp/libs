using System;

namespace DProjects.Factories.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class MinimumAttribute : Attribute {
        public double Min { get; }
        public bool Inclusive { get; }
        public MinimumAttribute(double min, bool inclusive = true) {
            Min = min;
            Inclusive = inclusive;
        }
    }


}