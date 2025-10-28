
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

        // Service extensions
        //public class Configuration(IServiceCollection services) {
        //}
        //public static IServiceCollection AddAuthProviders(this IServiceCollection services, Action<Configuration> configuration) {
        //    var config = new Configuration(services);
        //    configuration.Invoke(config);
        //    return services;
        //}


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
        //public static IConfigurationBuilder ReplaceSecrets(this ConfigurationManager configuration, string? url = null) {
        //    // replace secrets in configuration
        //    ISecretProvider? secretProvider = null;
        //    if (url != null) {
        //        secretProvider = CreateSecretProvider(url);
        //    } else if (configuration["Secrets"] != null) {
        //        secretProvider = CreateSecretProvider(configuration["Secrets"]!);
        //    }
        //    // scan all configuration keypairs
        //    var now = DateTime.Now;
        //    var items = new Dictionary<string, string>();
            
        //    foreach (var child in configuration.AsEnumerable()) {
        //        if (child.Value != null) {
        //            var i = child.Value.IndexOf("${");
        //            if (i != -1) {
        //                var value = child.Value;
        //                do {
        //                    try {
        //                        int j = value.IndexOf("}", i);
        //                        if (j == -1) break;
        //                        var key = value.Substring(i + 2, j - i - 2);
        //                        var replacement = "";
        //                        if (key.Equals("date")) {
        //                            replacement = now.ToString(DateTimeUtils.DATETIME_ISO8601_DATE);
        //                        } else if (key.Equals("datetime")) {
        //                            replacement = now.ToString(DateTimeUtils.DATETIME_ISO8601);
        //                        } else if (key.Equals("time")) {
        //                            replacement = now.ToString(DateTimeUtils.DATETIME_ISO8601_TIME);

        //                        } else if (key.Equals("year")) {
        //                            replacement = now.Year.ToString("D2");
        //                        } else if (key.Equals("month")) {
        //                            replacement = now.Month.ToString("D2");
        //                        } else if (key.Equals("day")) {
        //                            replacement = now.Day.ToString("D2");

        //                        } else if (key.Equals("hour")) {
        //                            replacement = now.Hour.ToString("D2");
        //                        } else if (key.Equals("minute")) {
        //                            replacement = now.Minute.ToString("D2");
        //                        } else if (key.Equals("second")) {
        //                            replacement = now.Second.ToString("D2");

        //                        } else if (key.Equals("pid")) {
        //                            replacement = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        //                        } else if (key.Equals("hostname")) {
        //                            replacement = System.Environment.MachineName;
        //                        } else if (key.Equals("username")) {
        //                            replacement = System.Environment.UserName;
        //                        } else if (key.Equals("cwd")) {
        //                            replacement = System.Environment.CurrentDirectory;

        //                        } else if (key.StartsWith("config:")) {
        //                            var variable = key.Substring(key.IndexOf(":") + 1);
        //                            if (items.ContainsKey(variable)) {
        //                                replacement = items[variable];
        //                            } else {
        //                                replacement = configuration.GetValue<string>(variable);
        //                            }
        //                        } else if (key.StartsWith("env:")) {
        //                            var variable = key.Substring(key.IndexOf(":")+1);
        //                            replacement = System.Environment.GetEnvironmentVariable(variable);
        //                        } else if (key.StartsWith("secret:") || key.StartsWith("password:")) {
        //                            var secretName = key.Substring(key.IndexOf(":") + 1);
        //                            var urlEncode = false;
        //                            if (secretName.EndsWith("|urlencode")) {
        //                                secretName = secretName.Substring(0, secretName.LastIndexOf("|"));
        //                                urlEncode = true;
        //                            }
        //                            if (secretProvider == null) {
        //                                throw new System.Exception($"Secret provider url not found in configuration: {url}");
        //                            } else {
        //                                var secret = secretProvider.Get(secretName);
        //                                if (secret == null) {
        //                                    throw new System.Exception($"Secret not found: {secretName}");
        //                                }
        //                                replacement = secret.GetValue();
        //                                if (urlEncode) {
        //                                    replacement = UrlUtils.UrlEncode(replacement);
        //                                }
        //                            }
        //                        } else {
        //                            replacement = configuration.GetValue<string>("Secrets:" + key, "");
        //                        }
        //                        if (replacement != null) {
        //                            value = value.Substring(0, i) + replacement + value.Substring(j + 1);
        //                            i = value.IndexOf("${", i + replacement.Length);
        //                        } else {
        //                            i = value.IndexOf("${", i + 1);
        //                        }
        //                    } catch (Exception e) {
        //                        throw new Exception("Error parsing: " + child.Value, e);
        //                    }
        //                } while (i != -1);
        //                items[child.Key] = value;
        //            }
        //        }
        //    }
        //    // add new memory collection keypairs that override the previous ones
        //    return configuration.AddInMemoryCollection(items.ToArray()!);
        //}
    }


}
