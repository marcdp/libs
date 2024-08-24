using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Identity.Membership {
    public class MembershipRole {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<MembershipClaim> Claims { get; } = new();

    }

}