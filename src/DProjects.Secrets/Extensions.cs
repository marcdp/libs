
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DProjects.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace DProjects.Secrets {


    public static class Extensions {
         

        // Configuration extensions
        private static string GetSecretsFilePath(string userSecretsId) {
            string basePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets");
            } else {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");
            }
            return Path.Combine(basePath, userSecretsId, "secrets.json");
        }
        public static ISecretProvider CreateSecretProvider(string url) {
            if (url.StartsWith("dprojectstools:")) {
                return new SecretProviderDProjectsToolsFactory().Create(url);
            } else if (url.StartsWith("user-secrets:")) {
                var path = GetSecretsFilePath(url.Substring(url.IndexOf(":") + 1));
                return new SecretProviderFileFactory().Create(path);
            } else if (url.StartsWith("file:")) {
                var path = url.Substring(url.IndexOf(":") + 1);
                return new SecretProviderFileFactory().Create(path);
            } else {
                throw new System.Exception($"Unknown secret provider: {url}");
            }
        } 
    }


}
