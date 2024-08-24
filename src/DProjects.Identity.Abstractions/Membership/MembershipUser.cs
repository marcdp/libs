using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace DProjects.Identity.Membership {


    public class MembershipUser {

        //props
        [JsonIgnore]
        public string Id => Identity.Id;
        [JsonIgnore]
        public string Name => Identity.Name;
        [JsonIgnore]
        public string Email => Identity.Email;
        [JsonIgnore]
        public string Phone => Identity.Phone;
        [JsonIgnore]
        public string Password => Identity.Password;
        [JsonIgnore]
        public string[] Roles => Identity.Roles;
        [JsonIgnore]
        public DateTime Created => Identity.Created;
        [JsonIgnore]
        public DateTime Modified => Identity.Modified;
        [JsonIgnore]
        public bool Disabled => Identity.Disabled;
        [JsonIgnore]
        public MembershipIdentity Identity {
            get {
                if (Identities.Count == 0) Identities.Add(new());
                return Identities.First();
            }
        }
        public List<MembershipIdentity> Identities { get; set; } = new();

        //methods
        public ClaimsPrincipal CreateClaimsPrincipal(string authenticationMethod) {
            var identities = new List<ClaimsIdentity>();
            foreach (var identity in Identities) {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, identity.Id),
                    new Claim(ClaimTypes.Name, identity.Name),
                    new Claim(ClaimTypes.Email, identity.Email),
                    new Claim(ClaimTypes.MobilePhone, identity.Phone),
                };
                if (identities.Count == 0) {
                    claims.Add(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
                    claims.Add(new Claim(ClaimTypes.AuthenticationInstant, DateTime.UtcNow.ToString("o")));
                }
                claims.AddRange(identity.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                claims.AddRange(identity.Claims.Select(claim => new Claim(claim.Type, claim.Value)));
                identities.Add(new ClaimsIdentity(claims, authenticationMethod));
            }
            return new ClaimsPrincipal(identities);
        }
    }
}