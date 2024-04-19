using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class ConfigurationUtils {


        //methods
        public static async Task ScanAndReplaceVariablesAsync(object instance, Func<string, Task<string>> handler) {
            var type = instance.GetType();
            if (instance is IDictionary<string, string>) {
                var dictionary = (IDictionary<string, string>)instance;
                foreach (var keyPair in dictionary) {
                    if (keyPair.Value != null) {
                        var newValue = await ScanStringAsync(keyPair.Value, handler);
                        if (keyPair.Value.Equals(newValue)) dictionary[keyPair.Key] = newValue;
                    }
                }
            } else if (ArrayUtils.IsArray(instance) && type.GetElementType() == typeof(string)) {
                for(int i = 0; i < ((Array)instance).Length; i++) {
                    var value = ((Array)instance).GetValue(i);
                    if (value != null) {
                        var newValue = await ScanStringAsync((string)value, handler);
                        if (value.Equals(newValue)) ((Array)instance).SetValue(newValue, i);
                    }
                }
            } else if (ArrayUtils.IsArray(instance) && type.GetElementType().IsClass) {
                foreach(var value in (Array)instance) {
                    if (value != null) await ScanAndReplaceVariablesAsync(value, handler);
                }
            } else {
                var properties = type.GetProperties();
                foreach (var property in properties) {
                    if (property.PropertyType == typeof(string)) {
                        var value = (string?)property.GetValue(instance);
                        if (value != null) {
                            var newValue = await ScanStringAsync(value, handler);
                            if (value != newValue) property.SetValue(instance, newValue);
                        }
                    } else if (property.PropertyType.IsClass) {
                        var value = property.GetValue(instance);
                        if (value != null) {
                            await ScanAndReplaceVariablesAsync(value, handler);
                        }
                    }
                }
            }
        }
        private static async Task<string> ScanStringAsync(string value, Func<string, Task<string>> handler) {
            var i = value.IndexOf("${");
            if (i != -1) {
                var originalValue = value;
                do {
                    try {
                        int j = value.IndexOf("}", i);
                        if (j == -1) break;
                        var key = value.Substring(i + 2, j - i - 2);
                        var replacement = await handler(key);
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
    }


}


