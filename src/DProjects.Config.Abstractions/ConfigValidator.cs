using System;
using System.ComponentModel.DataAnnotations;

namespace DProjects.Config {


    public class ConfigValidator {
        public static T ValidateAndThrow<T>(T config) {
            // validate all fields
            var type = typeof(T);
            foreach (var constructorInfo in type.GetConstructors()) {
                var parameterInfos = constructorInfo.GetParameters();
                var arguments = new object[parameterInfos.Length];
                for (var i = 0; i < parameterInfos.Length; i++) {
                    var parameterInfo = parameterInfos[i];
                    var argument = parameterInfo.DefaultValue;
                    var value = type.GetProperty(parameterInfo.Name)?.GetValue(config);
                    foreach(var attribute in parameterInfo.GetCustomAttributes(true)) {
                        if (attribute is ValidationAttribute validationAttribute) {
                            validationAttribute.Validate(value, parameterInfo.Name);
                        }
                    }
                }
            }
            // return
            return config;
        }

    }

}