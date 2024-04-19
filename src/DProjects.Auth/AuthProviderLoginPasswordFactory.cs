
using DProjects.Factories;
using DProjects.Factories.Attributes;


namespace DProjects.Auth {

    [Protocol("login-password", "")]
    public class AuthProviderLoginPasswordFactory() : IFactoryByUrl<IAuthProvider> {
        public IAuthProvider Create(string src) {
            return new AuthProviderLoginPassword();
        }

    }

}
