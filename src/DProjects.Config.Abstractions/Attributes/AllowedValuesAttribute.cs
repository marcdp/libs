using System;
using System.Collections.Generic;
using System.Linq;

namespace DProjects.Config.Attributes {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field |    AttributeTargets.Parameter)]
    public sealed class AllowedValuesAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute {

        // props
        public IReadOnlyList<object> Values { get; }

        // ctor
        public AllowedValuesAttribute(params object[] values) {
            Values = values;
        }

        // methods
        public override bool IsValid(object? value) {
            return value is null || Values.Contains(value);
        }

    }
}