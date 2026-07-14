using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

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
            if(value == null) return true;

            if (value is not IConvertible)
                throw new ValidationException($"{nameof(MaximumAttribute)} can only be applied to numeric values.");

            double number;

            try {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            } catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) {
                throw new ValidationException($"{nameof(MaximumAttribute)} can only be applied to numeric values.", ex);
            }

            return Inclusive
                ? number <= Max
                : number < Max;
        }
    }


}