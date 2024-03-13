using System;
using System.Security.Cryptography.X509Certificates;

namespace DProjects.Vault {

    public static class VaultExtensions {

        public static string GetSecret(this IVault vault, string path) {
            throw new NotImplementedException();
        }
        public static byte[] GetKey(this IVault vault, string path) {
            throw new NotImplementedException();
        }
        public static X509Certificate2 GetCertficate(this IVault vault, string path) {
            throw new NotImplementedException();
        }

    }

}