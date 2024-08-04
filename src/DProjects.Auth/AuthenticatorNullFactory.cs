
using DProjects.Factories;
using DProjects.Factories.Attributes;


namespace DProjects.Auth {

    [Protocol("null", "")]
    [ProtocolExample("null:", "")]
    public class AuthenticatorNullFactory() : IFactoryByUrl<IAuthenticator> {
        public IAuthenticator Create(string src) {
            return new AuthenticatorNull();
        }

    }

}
