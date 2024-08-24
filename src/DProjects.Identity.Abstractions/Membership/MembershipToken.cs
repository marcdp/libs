using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Identity.Membership {

    public class MembershipToken  {

        public MembershipToken() {
        }
        public MembershipToken(string name, string value) {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
    }

}