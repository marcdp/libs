using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault {

    public static class ExtensionsUtils {

        // configuration builder
        public static IConfigurationBuilder AddXVaultFile(this ConfigurationManager configuration, string path, string? password = null) {
            var xvault = new XVault(path, password);
            xvault.Register(configuration);

            return configuration;
        }
    }

}