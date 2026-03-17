using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using DProjects.Utils;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Utils {
     

    public static class ConfigurationUtils {

        // configuration builder
        public static IConfigurationBuilder ReplaceVariables(this Microsoft.Extensions.Configuration.ConfigurationManager configuration, Func<string, string?> handler) {
            ScanAndReplaceVariables(configuration, handler);
            return configuration;
            //// scan all configuration keypairs
            //var items = new Dictionary<string, string>();
            //foreach (var child in configuration.AsEnumerable()) {
            //    if (child.Value != null) {
            //        var i = child.Value.IndexOf("${");
            //        if (i != -1) {
            //            var value = child.Value;
            //            items[child.Key] = value;
            //        }
            //    }
            //}
            //// add new memory collection keypairs that override the previous ones
            //ConfigurationUtils.ScanAndReplaceVariables(items, handler);
            //var itemsList = new List<KeyValuePair<string, string>>();
            //foreach (var kvp in items) {
            //    itemsList.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
            //}
            //return configuration.AddInMemoryCollection(itemsList.ToArray()!);
        }


        //methods        
        public static void ScanAndReplaceVariables(object instance, Func<string, string?> handler) {
            var type = instance.GetType();
            if (instance is IDictionary<string, string>) {
                var dictionary = (IDictionary<string, string>)instance;
                foreach (var keyPair in dictionary) {
                    if (keyPair.Value != null) {
                        var newValue = ScanString(keyPair.Value, handler);
                        if (!keyPair.Value.Equals(newValue)) dictionary[keyPair.Key] = newValue;
                    }
                }
            } else if (ArrayUtils.IsArray(instance) && type.GetElementType() == typeof(string)) {
                for (int i = 0; i < ((Array)instance).Length; i++) {
                    var value = ((Array)instance).GetValue(i);
                    if (value != null) {
                        var newValue = ScanString((string)value, handler);
                        if (!value.Equals(newValue)) ((Array)instance).SetValue(newValue, i);
                    }
                }
            } else if (ArrayUtils.IsArray(instance) && type.GetElementType().IsClass) {
                foreach (var value in (Array)instance) {
                    if (value != null) ScanAndReplaceVariables(value, handler);
                }
            } else if (instance is Microsoft.Extensions.Configuration.ConfigurationManager configurationManager) {
                var items = new Dictionary<string, string>();
                foreach (var child in configurationManager.AsEnumerable()) {
                    if (child.Value != null) {
                        var i = child.Value.IndexOf("${");
                        if (i != -1) {
                            var value = child.Value;
                            items[child.Key] = value;
                        }
                    }
                }
                ScanAndReplaceVariables(items, handler);
                var itemsList = new List<KeyValuePair<string, string>>();
                foreach (var kvp in items) {
                    itemsList.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                }
                configurationManager.AddInMemoryCollection(itemsList.ToArray()!);
            } else {
                var properties = type.GetProperties();
                foreach (var property in properties) {
                    if (property.PropertyType == typeof(string)) {
                        var value = (string?)property.GetValue(instance);
                        if (value != null) {
                            var newValue = ScanString(value, handler);
                            if (value != newValue) {
                                property.SetValue(instance, newValue);
                            }
                        }
                    } else if (property.PropertyType.IsClass) {
                        var value = property.GetValue(instance);
                        if (value != null) {
                            ScanAndReplaceVariables(value, handler);
                        }
                    }
                }
            }
        }
        private static string ScanString(string value, Func<string, string?> handler) {
            // scan for ${...} patterns in string, and resolves its value
            var i = value.IndexOf("${");
            if (i != -1) {
                var originalValue = value;
                do {
                    try {
                        int j = value.IndexOf("}", i);
                        if (j == -1) break;
                        var key = value.Substring(i + 2, j - i - 2);
                        var replacement = ResolveVariable(key, handler);
                        if (replacement != null) {
                            value = value.Substring(0, i) + replacement + value.Substring(j + 1);
                            i = value.IndexOf("${", i + replacement.Length);
                        } else {
                            i = value.IndexOf("${", i + 1);
                        }
                    } catch (Exception e) {
                        throw new Exception("Error parsing: " + originalValue, e);
                    }
                } while (i != -1);
            }
            return value;
        }
        private static string? ResolveVariable(string key, Func<string, string?> handler) {

            // date
            var now = System.DateTime.Now;
            if (key.Equals("date")) {
                return now.ToString(DateTimeUtils.DATETIME_ISO8601_DATE);
            } else if (key.Equals("datetime")) {
                return now.ToString(DateTimeUtils.DATETIME_ISO8601);
            } else if (key.Equals("time")) {
                return now.ToString(DateTimeUtils.DATETIME_ISO8601_TIME);
            } else if (key.Equals("year")) {
                return now.Year.ToString("D2");
            } else if (key.Equals("month")) {
                return now.Month.ToString("D2");
            } else if (key.Equals("day")) {
                return now.Day.ToString("D2");
            } else if (key.Equals("hour")) {
                return now.Hour.ToString("D2");
            } else if (key.Equals("minute")) {
                return now.Minute.ToString("D2");
            } else if (key.Equals("second")) {
                return now.Second.ToString("D2");
            }

            // host, user
            if (key.Equals("pid")) {
                return System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            } else if (key.Equals("hostname")) {
                return System.Environment.MachineName;
            } else if (key.Equals("username")) {
                return System.Environment.UserName;
            } else if (key.Equals("cwd")) {
                return System.Environment.CurrentDirectory;
            }

            // env
            if (key.StartsWith("env:")) {
                var variable = key.Substring(key.IndexOf(":") + 1);
                return System.Environment.GetEnvironmentVariable(variable);
            }

            // return null
            return handler(key);
        }
    }


}


