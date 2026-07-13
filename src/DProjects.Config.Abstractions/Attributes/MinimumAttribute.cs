using System;

namespace DProjects.Config.Attributes {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class MinimumAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute {

        // props
        public double Min { get; }
        public bool Inclusive { get; }

        // ctor
        public MinimumAttribute(double min, bool inclusive = true) {
            Min = min;
            Inclusive = inclusive;
        }

        // methods
        public override bool IsValid(object? value) {
            throw new NotImplementedException();
        }

    }
}