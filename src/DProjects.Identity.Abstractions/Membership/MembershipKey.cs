using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Identity.Membership {

    public class MembershipKey {

        public MembershipKey() { 
        }
        public MembershipKey(string name, string value) {
            Name = name;
            Value = value;
        }
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
    }

}