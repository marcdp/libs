using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DProjects.Auth {

    public class Identity : System.Security.Principal.IIdentity {

        //ctor
        public Identity(string name, string authenticationType, bool isAuthenticated, string[] roles, IReadOnlyDictionary<string,string> claims) {
            Name = name;
            AuthenticationType = authenticationType;
            IsAuthenticated = isAuthenticated;
            Roles = roles;
            Claims = claims;
        }

        //props
        public string Name { get; private set; }
        public string[] Roles { get; private set; }
        public string AuthenticationType { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public IReadOnlyDictionary<string, string> Claims { get; private set; }

        //methods
        public T GetClaim<T>(string claimType, T defaultValue) {
            if (Claims.ContainsKey(claimType)) {
                return ConvertUtils.To<T>(Claims[claimType]);
            }
            return defaultValue;
        }
        public static Identity Anonymous() {
            return new Identity("anonymous", "", false, [], new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()));
        }
    }

}