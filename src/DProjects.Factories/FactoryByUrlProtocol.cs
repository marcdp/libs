using DProjects.Factories.Attributes;
using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DProjects.Factories {


    // class
    public class FactoryByUrlProtocol<TType> : IComparable<FactoryByUrlProtocol<TType>> where TType  : class {

        //variables
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public string Help { get; }
        public IDictionary<string, string> Examples { get; }
        public ProtocolParameterAttribute[] Parameters { get; }
        public Type Factory { get; }
        

        //constructor
        public FactoryByUrlProtocol(Type factory, string name, string description, string usage, IDictionary<string, string> examples, string help, ProtocolParameterAttribute[] parameters) {
            Name = name;
            Description = description;
            Usage = usage;
            Help = help;
            Examples = examples;
            Factory = factory;
            Parameters = parameters;
        } 
        public FactoryByUrlProtocol(Type factory) {
            var protocolAttribute = factory.GetCustomAttribute<ProtocolAttribute>();
            Name = protocolAttribute.Name;
            // description
            Description = protocolAttribute.Description;
            //  examples
            var examples = new Dictionary<string, string>();
            foreach (var protocolExampleAttribute in factory.GetCustomAttributes<ProtocolExampleAttribute>()) {
                examples.Add(protocolExampleAttribute.Value, protocolExampleAttribute.Description);
            }
            Examples = examples;
            // parameters
            var parameters = new List<ProtocolParameterAttribute>();
            var protocolParametersAttribute = factory.GetCustomAttribute<ProtocolConfigAttribute>();
            if (protocolParametersAttribute != null) {
                var parametersType = protocolParametersAttribute.Type;
                var ctor = parametersType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
                foreach (var param in ctor.GetParameters()) {
                    var paramName = StringUtils.UnCapitalizeFirstChar(param.Name!);
                    var paramType = param.ParameterType;
                    var paramDescription = param.GetCustomAttribute<ProtocolConfigPropertyAttribute>()?.Description ?? "";
                    var paramHelp = param.GetCustomAttribute<ProtocolConfigPropertyAttribute>()?.Help;
                    var paramDefault = param.HasDefaultValue ? param.DefaultValue : null;
                    var paramExamples = new Dictionary<string, string>();
                    //foreach(var protocolConfigPropertyExample in param.GetCustomAttributes<ProtocolConfigPropertyExampleAttribute>()) {
                    //    paramExamples.Add(protocolConfigPropertyExample.Value, protocolConfigPropertyExample.Description);
                    //}
                    //var paramValidations = param.GetCustomAttributes<ProtocolConfigPropertyValidationAttribute>().ToArray();
                    parameters.Add(new ProtocolParameterAttribute(paramName, paramType, paramDescription, paramDefault) { Examples = paramExamples, Help = paramHelp  });
                }
            }
            foreach (var protocolParameterAttribute in factory.GetCustomAttributes<ProtocolParameterAttribute>()) {
                parameters.Add(protocolParameterAttribute);
            }
            Parameters = parameters.ToArray();
            // usage
            var protocolUsageAttribute = factory.GetCustomAttribute<ProtocolUsageAttribute>();
            Usage = protocolUsageAttribute?.Usage ?? "";
            if (string.IsNullOrEmpty(Usage) && Parameters.Length > 0) {
                var usageBuilder = new StringBuilder();
                usageBuilder.Append(StringUtils.UnCapitalizeFirstChar(Name) + ":?");
                for (int i = 0; i < Parameters.Length; i++) {
                    var parameter = Parameters[i];
                    if (parameter.Required) {
                        if (i > 0) {
                            usageBuilder.Append("&");
                        }
                        usageBuilder.Append(StringUtils.UnCapitalizeFirstChar(parameter.Name) + "=<" + parameter.Name.ToLower() + ">");
                    }
                }
                for (int i = 0; i < Parameters.Length; i++) {
                    var parameter = Parameters[i];
                    if (!parameter.Required) {
                        usageBuilder.Append("[" + (i > 0 ? "&" : "") + StringUtils.UnCapitalizeFirstChar(parameter.Name) + "=" + parameter.DefaultValue + "]");
                    }
                }
                Usage = usageBuilder.ToString();
            }
            // help
            var protocolHelpAttribute = factory.GetCustomAttribute<ProtocolHelpAttribute>();
            Help = protocolHelpAttribute?.Help ?? "";
            // factory
            Factory = factory;
        }

        //methods
        public int CompareTo(FactoryByUrlProtocol<TType> other) {
            return Name.CompareTo(other.Name);
        }
        public override string ToString() {
            return Name + ":";
        }
        public string ToYaml(string? type = null) {
            var yaml = new StringBuilder();
            if (!string.IsNullOrEmpty(type)) {
                yaml.AppendLine("type: " + type);
            }
            yaml.AppendLine("protocol: " + Name);
            yaml.AppendLine("category: " + Factory.Namespace!.Split('.').Last());
            if (!string.IsNullOrEmpty(Description)) {
                yaml.AppendLine("description: " + Description);
            }            
            if (!string.IsNullOrEmpty(Usage)) {
                yaml.AppendLine("usage: " + Usage);
            }
            if (Parameters.Length > 0) {
                yaml.AppendLine("parameters:");
                foreach (var parameter in Parameters) {
                    var parameterInfo = new Dictionary<string, object>();
                    yaml.AppendLine("- name: " + parameter.Name);
                    yaml.AppendLine("  type: " + parameter.Type.Name.ToLower());
                    yaml.AppendLine("  required: " + (parameter.Required ? "true" : "false"));
                    yaml.AppendLine("  defaultValue: " + (parameter.DefaultValue ?? ""));
                    yaml.AppendLine("  description: " + (parameter.Description ?? ""));
                    if (!string.IsNullOrEmpty(parameter.Help)) {
                        yaml.AppendLine("  help: >-\n    " + parameter.Help!.Replace("\r\n", "\n").Replace("\n", "\n    "));
                    }
                    //if (parameter.Validations != null && parameter.Validations.Length > 0) {
                    //    yaml.AppendLine("  validations: ");
                    //    foreach (var validation in parameter.Validations) {
                    //        yaml.AppendLine("  - type: " + validation.GetType().Name.Replace("ProtocolConfigProperty", "").Replace("Attribute", ""));
                    //        yaml.AppendLine("    description: " + validation.GetDescription());
                    //        foreach (var key in validation.GetValues().Keys) {
                    //            yaml.AppendLine("    " + key + ": " + validation.GetValues()[key].ToString().Replace("True", "true").Replace("False", "false"));
                    //        }
                    //    }
                    //    if (parameter.Examples != null && parameter.Examples.Count > 0) {
                    //        yaml.AppendLine("  examples: ");
                    //        foreach (var example in parameter.Examples) {
                    //            yaml.AppendLine("  - example: " + example.Key);
                    //            yaml.AppendLine("    description: " + example.Value);
                    //        }
                    //    }
                    //}
                }
            }
            if (!string.IsNullOrEmpty(Help)) {
                yaml.AppendLine("help: >-\n  " + Help.Replace("\r\n", "\n").Replace("\n", "\n  "));
            } 
            if (Examples != null && Examples.Count > 0) {
                yaml.AppendLine("examples: ");
                foreach (var example in Examples) {
                    yaml.AppendLine("- example: " + example.Key);
                    yaml.AppendLine("  description: " + example.Value);
                }
            }
            return yaml.ToString();
        }
    }

    public class FactoryByUrlProtocol<TType, TArgument> : IComparable<FactoryByUrlProtocol<TType, TArgument>> where TType : class where TArgument : class {

        //variables
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public string Help { get; }
        public IDictionary<string, string> Examples { get; }
        public ProtocolParameterAttribute[] Parameters { get; }
        public Type Factory { get; }


        //constructor
        public FactoryByUrlProtocol(Type factory, string name, string description, string usage, IDictionary<string, string> examples, string help, ProtocolParameterAttribute[] parameters) {
            Name = name;
            Description = description;
            Usage = usage;
            Examples = examples;
            Help = help;
            Factory = factory;
            Parameters = parameters;
        }
        public FactoryByUrlProtocol(Type factory) {
            var protocolAttribute = factory.GetCustomAttribute<ProtocolAttribute>();
            Name = protocolAttribute.Name;
            // description
            Description = protocolAttribute.Description;
            // examples
            var examples = new Dictionary<string, string>();
            foreach (var protocolExampleAttribute in factory.GetCustomAttributes<ProtocolExampleAttribute>()) {
                examples.Add(protocolExampleAttribute.Value, protocolExampleAttribute.Description);
            }
            Examples = examples;
            // parameters
            var parameters = new List<ProtocolParameterAttribute>();
            var protocolParametersAttribute = factory.GetCustomAttribute<ProtocolConfigAttribute>();
            if (protocolParametersAttribute != null) {
                var parametersType = protocolParametersAttribute.Type;
                var ctor = parametersType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
                foreach (var param in ctor.GetParameters()) {
                    var paramName = StringUtils.UnCapitalizeFirstChar(param.Name!);
                    var paramType = param.ParameterType;
                    var paramDescription = param.GetCustomAttribute<ProtocolConfigPropertyAttribute>()?.Description ?? "";
                    var paramDefault = param.HasDefaultValue ? param.DefaultValue : null;
                    var paramHelp = param.GetCustomAttribute<ProtocolConfigPropertyAttribute>()?.Help;
                    var paramExamples = new Dictionary<string, string>();
                    //foreach (var protocolConfigPropertyExample in param.GetCustomAttributes<ProtocolConfigPropertyExampleAttribute>()) {
                    //    paramExamples.Add(protocolConfigPropertyExample.Value, protocolConfigPropertyExample.Description);
                    //}
                    parameters.Add(new ProtocolParameterAttribute(paramName, paramType, paramDescription, paramDefault) { Examples = paramExamples, Help = paramHelp });
                }
            }
            foreach (var protocolParameterAttribute in factory.GetCustomAttributes<ProtocolParameterAttribute>()) {
                parameters.Add(protocolParameterAttribute);
            }
            Parameters = parameters.ToArray();
            var protocolHelpAttribute = factory.GetCustomAttribute<ProtocolHelpAttribute>();
            // usage
            var protocolUsageAttribute = factory.GetCustomAttribute<ProtocolUsageAttribute>();
            Usage = protocolUsageAttribute?.Usage ?? "";
            if (string.IsNullOrEmpty(Usage) && Parameters.Length > 0) {
                var usageBuilder = new StringBuilder();
                usageBuilder.Append(StringUtils.UnCapitalizeFirstChar(Name) + ":?");
                for (int i = 0; i < Parameters.Length; i++) {
                    var parameter = Parameters[i];
                    usageBuilder.Append(StringUtils.UnCapitalizeFirstChar(parameter.Name) + "=<" + parameter.Name.ToUpper() + ">");
                    if (i < Parameters.Length - 1) {
                        usageBuilder.Append("&");
                    }
                }
                Usage = usageBuilder.ToString();
            }
            // help
            Help = protocolHelpAttribute?.Help ?? "";   
            // factory
            Factory = factory;
        }

        //methods
        public int CompareTo(FactoryByUrlProtocol<TType, TArgument> other) {
            return Name.CompareTo(other.Name);
        }
        public override string ToString() {
            return Name + ":";
        }
    }
}