using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

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
            if (value == null) return true;

            if (value is not IConvertible)
                throw new ValidationException($"{nameof(MinimumAttribute)} can only be applied to numeric values.");

            double number;

            try {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            } catch (Exception ex) {
                throw new ValidationException($"{nameof(MinimumAttribute)} can only be applied to numeric values.", ex);
            }

            return Inclusive
                ? number >= Min
                : number > Min;
        }

    }
}