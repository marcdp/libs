
using System.Collections.Generic;
using System.Security.Claims;

using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;


namespace DProjects.Identity.SignIn {

    [Protocol("static", "")]
    [ProtocolExample("static://mylogin:mypass@therealm?tries=3&role=xxxxx&role=kkkkk&claim1=...&claim2=...", "")]
    public class SignInStaticFactory() : IFactoryByUrl<ISignIn> {
        public ISignIn Create(string src) {
            var aUrl = new System.Uri(src);
            var login = UrlUtils.UrlDecode(aUrl.UserInfo.Split(':')[0]);
            var password = UrlUtils.UrlDecode((aUrl.UserInfo + ":").Split(':')[1]);
            var realm = aUrl.Host;  
            var tries = UrlUtils.GetQueryValue<int>(aUrl.Query, "tries", 1);
            var claims = new List<Claim>();
            var query = UrlUtils.ParseQueryString(aUrl.Query);
            foreach (var key in query.AllKeys) {
                if (key.Equals("tries") || key.Equals("roles")) continue;
                foreach(var value in query.GetValues(key)) {
                    claims.Add(new Claim(key, value));
                }
            }
            return new SignInStatic(login, password, realm, claims.ToArray(), tries);
        }

    }

}
