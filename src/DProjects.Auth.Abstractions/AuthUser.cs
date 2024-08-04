using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Auth {

    public class AuthUser {

        //vars
        private ClaimsPrincipal mPrincipal;

        //ctor
        public AuthUser(ClaimsPrincipal principal) {
            mPrincipal = principal;
        }

        //props
        public string Id => GetClaim<string>(ClaimTypes.NameIdentifier, "");
        public string Name => GetClaim<string>(ClaimTypes.Name, "");
        public string Email => GetClaim<string>(ClaimTypes.Email, "");
        public bool IsAuthenticated => mPrincipal.Identity != null && mPrincipal.Identity.IsAuthenticated;
        public ClaimsPrincipal Principal => mPrincipal;

        //methods        
        public T GetClaim<T>(string claimType, T? defaultValue)  {
            return defaultValue!;
        }

    }

}