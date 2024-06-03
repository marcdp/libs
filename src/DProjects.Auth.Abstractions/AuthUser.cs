using System.Collections.Generic;
using System.Security.Claims;

namespace DProjects.Auth {

    public class AuthUser {


        //vars
        private ClaimsPrincipal mPrincipal;


        //ctor
        public AuthUser(ClaimsPrincipal? principal = null) {
            mPrincipal = principal ?? new();
        }

        //props
        public string Id => GetClaim<string>(ClaimTypes.NameIdentifier, "");
        public string Name => GetClaim<string>(ClaimTypes.Name, "");
        public string Email => GetClaim<string>(ClaimTypes.Email, "");
        //public string[] Roles { get; private set; }
        //public string AuthenticationType { get; private set; }
        public bool IsAuthenticated => mPrincipal.Identity != null && mPrincipal.Identity.IsAuthenticated;
        public IEnumerable<Claim> Claims => mPrincipal.Claims;

        //methods
        
        public T GetClaim<T>(string claimType, T? defaultValue)  {
            //mPrincipal.Claims.Select(x => x.Type).ToList().ForEach(x => System.Console.WriteLine(x));   
            //if (mPrincipal.Claims.se.ContainsKey(claimType)) {
            //    return ConvertUtils.To<T>(Claims[claimType]);
            //}
            return defaultValue!;
        }
        public void Set(ClaimsPrincipal principal) {
            mPrincipal = principal;
        }
    }

}