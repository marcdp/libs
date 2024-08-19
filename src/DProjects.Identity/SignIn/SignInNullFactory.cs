
using DProjects.Factories;
using DProjects.Factories.Attributes;


namespace DProjects.Identity.SignIn {

    [Protocol("null", "")]
    [ProtocolExample("null:", "")]
    public class SignInNullFactory() : IFactoryByUrl<ISignIn> {
        public ISignIn Create(string src) {
            return new SignInNull();
        }

    }

}
