using System;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using DProjects.Fs;


namespace DProjects.Secrets {

    [Protocol("null", "")]
    public class SecretManagerNullFactory() : IFactoryByUrl<ISecretManager> {
        public ISecretManager Create(string src) {
            return new SecretManagerNull();
        }
    }







}
