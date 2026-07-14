using System;

namespace DProjects.Config.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class MaximumAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute {
        
        
        //props
        public double Max { get; }
        public bool Inclusive { get; }

        // ctor
        public MaximumAttribute(double max, bool inclusive = true) {
            Max = max;
            Inclusive = inclusive;
        }

        // methods
        public override bool IsValid(object? value) {
            throw new NotImplementedException();
        }
    }


}