using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Identity.Membership {

    public class MembershipClaim {
        public MembershipClaim() {
        }
        public MembershipClaim(string type, string value, string? valueType = null) {
            Type = type;
            Value = value;
            ValueType = valueType ?? ClaimValueTypes.String;
        }
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public string ValueType { get; set; } = ClaimValueTypes.String;
        public DateTime Created { get; set; } = DateTime.Now;
    }

}