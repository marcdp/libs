using System;
using System.Collections.Generic;
using System.Security.Claims;

using DProjects.Utils;

namespace DProjects.Identity.Membership {

    public class MembershipIdentity {


        //props
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public string Password { get; set; } = string.Empty;
        public DateTime Created { get; set; } = default;
        public DateTime Modified { get; set; } = default;
        public bool Disabled { get; set; } = false;
        public List<MembershipClaim> Claims { get; set; } = new();
        public List<MembershipToken> Tokens { get; set; } = new();
        public List<MembershipKey> Keys { get; set; } = new();


        //methods
        public T? GetClaimValue<T>(string type, T? defaultValue = default) {
            var value = GetClaim(type);
            if (value == null) return defaultValue;
            return ConvertUtils.To<T>(value);
        }
        public string? GetClaim(string type) {
            foreach (var claim in Claims) {
                if (claim.Type.Equals(type)) return claim.Value;
            }
            return null;
        }
        public string? GetKey(string name) {
            foreach (var key in Keys) {
                if (key.Name.Equals(name)) return key.Value;
            }
            return null;
        }
        public string? GetToken(string name) {
            foreach (var token in Tokens) {
                if (token.Name.Equals(name)) return token.Value;
            }
            return null;
        }

    }

}