
using DProjects.Utils;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using System;
using System.Collections.Generic;
using DProjects.CommandLine.Attributes;
using System.Threading.Tasks;
using System.IO;


namespace DProjects.CommandLine.Schema {

    public class CmdSchemaDefinition {


        //constants
        public const string HYPHEN_PREFIX = "[HYPHEN]";


        //properties
        public string Location { get; set; } = "";
        public string Name { get; set; } = "";
        [JsonIgnore] public Type? Handler { get; set; } = null;
        public bool History { get; set; } = true;
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Help { get; set; } = "";
        public string? Module { get; set; } = "";
        public CmdSchemaFlag[] Flags { get; set; } = new CmdSchemaFlag[] { };
        public CmdSchemaArgument[] Arguments { get; set; } = new CmdSchemaArgument[] { };
        public CmdSchemaBody? Body { get; set; } = null;
        public CmdSchemaRaw? Raw { get; set; } = null;
        public CmdSchemaRemaining? Remaining { get; set; } = null;
        public CmdSchemaSynopsis[] Synopsis { get; set; } = new CmdSchemaSynopsis[] { };
        public CmdSchemaExitCode[] ExitCodes { get; set; } = new CmdSchemaExitCode[] { };
        public CmdSchemaExample[] Examples { get; set; } = new CmdSchemaExample[] { };
        public CmdSchemaTag[] Tags { get; set; } = new CmdSchemaTag[] { };


        //command
        public CmdSchemaDefinition() { }


        //static methods
        public static string GetCommandName(Type type) {
            var key = StringUtils.CamelToKebabCase(type.Name, true);
            return key;
        }
        public static CmdSchemaDefinition Create(Type handler, string location, string type, bool addHelpTag, string? module) {
            var result = new CmdSchemaDefinition();
            //name
            var name = StringUtils.CamelToKebabCase(handler.Name, true);
            var nameAttribute = handler.GetTypeInfo().GetCustomAttribute<NameAttribute>(false);
            result.Name = (nameAttribute != null ? nameAttribute.Name : name);
            //if (!string.IsNullOrEmpty(forcedName)) result.Name = forcedName ?? "";
            //location
            result.Location = location;
            //handler
            result.Handler = handler;
            //type
            result.Type = type;
            //description
            var descriptionAttribute = handler.GetTypeInfo().GetCustomAttribute<DescriptionAttribute>(false);
            result.Description = (descriptionAttribute != null ? descriptionAttribute.Description : "");
            //history
            var historyAttribute = handler.GetTypeInfo().GetCustomAttribute<HistoryAttribute>(false);
            result.History = (historyAttribute == null ? true : historyAttribute.Value);
            //help
            var helpAttribute = handler.GetTypeInfo().GetCustomAttribute<HelpAttribute>(false);
            result.Help = (helpAttribute != null ? helpAttribute.Help : "");
            //category
            var categoryAttribute = handler.GetTypeInfo().GetCustomAttribute<CategoryAttribute>(false);
            result.Category = (categoryAttribute != null ? categoryAttribute.Category : "");
            if (result.Category.Length == 0) {
                var typeName = handler.FullName;
                if (typeName != null && typeName.IndexOf(".") != -1) {
                    string ns = typeName.Substring(0, typeName.LastIndexOf("."));
                    ns = ns.Replace(".Cmd", "");
                    if (ns.IndexOf("Plugins.") != -1) {
                        ns = ns.Substring(ns.LastIndexOf("Plugins.") + 8).Replace(".", "/");
                    } else {
                        ns = ns.Substring(ns.LastIndexOf(".") + 1);
                    }
                    result.Category = StringUtils.CamelToKebabCase(ns, true);
                }
            }
            //flags
            var cmdSchemaFlags = new List<CmdSchemaFlag>();
            foreach (PropertyInfo propertyInfo in handler.GetProperties()) {
                var flagAttribute = propertyInfo.GetCustomAttribute<FlagAttribute>(false);
                if (flagAttribute != null) {
                    var domain = (propertyInfo.PropertyType.GetTypeInfo().IsEnum ? System.Enum.GetNames(propertyInfo.PropertyType) : null);
                    cmdSchemaFlags.Add(new CmdSchemaFlag() {
                        Name = propertyInfo.Name,
                        Char = flagAttribute.Char,
                        Type = propertyInfo.PropertyType,
                        Description = flagAttribute.Description,
                        Default = flagAttribute.Default,
                        Required = (flagAttribute.Default == null),
                        Domain = domain,
                        Alias = flagAttribute.Alias,
                        PropertyInfo = propertyInfo
                    });
                }
            }
            if (addHelpTag) {
                cmdSchemaFlags.Add(new CmdSchemaFlag() {
                    Name = "Help",
                    Char = 'h',
                    Type = typeof(bool),
                    Description = "Show help message",
                    Default = false
                });
            }
            result.Flags = cmdSchemaFlags.ToArray();
            //arguments
            var cmdSchemaArguments = new List<CmdSchemaArgument>();
            foreach (var propertyInfo in handler.GetProperties()) {
                var argumentAttribute = propertyInfo.GetCustomAttribute<ArgumentAttribute>(false);
                if (argumentAttribute != null) {
                    cmdSchemaArguments.Add(new CmdSchemaArgument() {
                        Name = propertyInfo.Name,
                        Type = propertyInfo.PropertyType,
                        Index = argumentAttribute.Index,
                        Description = argumentAttribute.Description,
                        Default = argumentAttribute.Default,
                        Required = (argumentAttribute.Default == null),
                        Domain = (propertyInfo.PropertyType.GetTypeInfo().IsEnum ? System.Enum.GetNames(propertyInfo.PropertyType) : null),
                        Alias = argumentAttribute.Alias,
                        PropertyInfo = propertyInfo
                    });
                }
            }
            cmdSchemaArguments.Sort((a, b) => {
                return a.Index.CompareTo(b.Index);
            });
            result.Arguments = cmdSchemaArguments.ToArray();
            //body
            foreach (var propertyInfo in handler.GetProperties()) {
                var bodyAttribute = propertyInfo.GetCustomAttribute<BodyAttribute>(false);
                if (bodyAttribute != null) {
                    result.Body = new CmdSchemaBody() {
                        Description = bodyAttribute.Description,
                        PropertyInfo = propertyInfo
                    };
                }
            }
            //raw
            foreach (var propertyInfo in handler.GetProperties()) {
                var rawAttribute = propertyInfo.GetCustomAttribute<RawAttribute>(false);
                if (rawAttribute != null) {
                    result.Raw = new CmdSchemaRaw() {
                        Type = propertyInfo.PropertyType,
                        Description = rawAttribute.Description,
                        Default = rawAttribute.Default,
                        AvoidExpansions = rawAttribute.AvoidExpansions,
                        PropertyInfo = propertyInfo
                    };
                }
            }
            //remaining
            foreach (var propertyInfo in handler.GetProperties()) {
                var remainingAttribute = propertyInfo.GetCustomAttribute<RemainingAttribute>(false);
                if (remainingAttribute != null) {
                    result.Remaining = new CmdSchemaRemaining() {
                        Type = propertyInfo.PropertyType,
                        Description = remainingAttribute.Description,
                        Default = remainingAttribute.Default,
                        AvoidExpansions = remainingAttribute.AvoidExpansions,
                        PropertyInfo = propertyInfo
                    };
                }
            }
            //synopsis
            var cmdSchemaSynopsis = new List<CmdSchemaSynopsis>();
            foreach (var synopsi in handler.GetCustomAttributes<SynopsisAttribute>()) {
                cmdSchemaSynopsis.Add(new CmdSchemaSynopsis() {
                    Example = synopsi.Example,
                    Description = synopsi.Description
                });
            }
            result.Synopsis = cmdSchemaSynopsis.ToArray();
            //exitCodes
            var cmdSchemaExitCodes = new List<CmdSchemaExitCode>();
            foreach (var exitCode in handler.GetCustomAttributes<ExitCodeAttribute>()) {
                cmdSchemaExitCodes.Add(new CmdSchemaExitCode() {
                    Code = exitCode.Code,
                    Description = exitCode.Description
                });
            }
            result.ExitCodes = cmdSchemaExitCodes.ToArray();
            //examples
            var cmdSchemaExamples = new List<CmdSchemaExample>();
            foreach (var example in handler.GetCustomAttributes<ExampleAttribute>()) {
                cmdSchemaExamples.Add(new CmdSchemaExample() {
                    Example = example.Example,
                    Description = example.Description
                });
            }
            result.Examples = cmdSchemaExamples.ToArray();
            //tags
            var cmdSchemaTags = new List<CmdSchemaTag>();
            foreach (var tag in handler.GetCustomAttributes<TagAttribute>()) {
                cmdSchemaTags.Add(new CmdSchemaTag(tag.Tag));
            }
            result.Tags = cmdSchemaTags.ToArray();
            //module
            result.Module = module;
            //return
            return result;
        }


        //initialize instance properties
        public bool InitializeObjectProperties(object instance, string[] args, object? body, string? sheBangArgsSeparator, IDictionary<string,string>? defaults, List<string> errors, Func<Type, string, object> getService) {
            var dictionary = instance as Dictionary<string, object>;
            var arguments = new List<string>(args);
            //set ArgumentBodyAttribute
            if (Body != null) {
                Body.PropertyInfo?.SetValue(instance, body);
            }
            //Raw
            if (Raw != null) {
                if (Raw.Type == typeof(string[])) {
                    if (arguments.Count == 0 && Raw.Default == null) {
                        errors.Add(Name + ": " + Raw.Description + ": required");
                    } else {
                        var value = new List<string>();
                        for (var i = 0; i < arguments.Count; i++) {
                            var argument = arguments[i];
                            if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                            value.Add(argument);
                        }
                        Raw.PropertyInfo?.SetValue(instance, value.ToArray());
                        arguments.Clear();
                    }
                } else {
                    var value = new StringBuilder();
                    if (arguments.Count == 0) value.Append(Raw.Default);
                    for (var i = 0; i < arguments.Count; i++) {
                        var argument = arguments[i];
                        if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                        if (value.Length > 0) value.Append(" ");
                        value.Append(argument);
                    }
                    Raw.PropertyInfo?.SetValue(instance, value.ToString());
                    arguments.Clear();
                }
            } else {                
                //flags
                foreach (var flag in this.Flags) {
                    var flagName = flag.Alias ?? StringUtils.CamelToKebabCase(flag.Name, true);
                    var value = flag.Default;
                    if (defaults != null) {
                        foreach (var def in defaults) {
                            if (def.Key.Equals(flagName, StringComparison.OrdinalIgnoreCase)) {
                                value = def.Value;
                            }                            
                        }
                    }
                    for (var i = 0; i < arguments.Count; i++) {
                        var argument = arguments[i];
                        if (argument.Equals(sheBangArgsSeparator)) {
                            break;
                        } else if (flag.Type == typeof(bool)) {
                            if (argument.Equals("-" + flag.Char) || argument.Equals("--" + flagName)) {
                                value = true;
                                arguments.RemoveAt(i);
                                break;
                            } else if (argument.StartsWith("-") && argument.Length > 2 && argument.Substring(1, 1) != "-" && argument.IndexOf(flag.Char) != -1) {
                                value = true;
                                argument = argument.Replace("" + flag.Char, "");
                                if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                                arguments[i] = argument;
                                if (argument == "-") arguments.RemoveAt(i);
                                break;
                            }
                        } else if (argument.StartsWith("-" + flag.Char + "=") || argument.StartsWith("--" + flagName + "=")) {
                            var subValue = argument.Substring(argument.IndexOf("=") + 1);                            
                            if (flag.Type == typeof(string[])) {
                                //multiple value (array)
                                var list = new List<string>();
                                if (flag.Default != null && value == flag.Default) {
                                } else {
                                    if (value != null) list.AddRange((string[])value);
                                }
                                list.Add(subValue);
                                value = list.ToArray();
                                arguments.RemoveAt(i);
                                i--;
                                continue;
                            } else if (flag.Type == typeof(int[])) {
                                //multiple value (array)
                                var list = new List<int>();
                                if (flag.Default != null && value == flag.Default) {
                                } else {
                                    if (value != null) list.AddRange((int[])value);
                                }
                                list.Add(int.Parse(subValue));
                                value = list.ToArray();
                                arguments.RemoveAt(i);
                                i--;
                                continue;
                            } else {
                                //single value
                                value = subValue;
                                arguments.RemoveAt(i);
                                i--;
                                break;
                            }
                        } else if ((argument.Equals("-" + flag.Char) || argument.Equals("--" + flagName))) {
                            if (i < arguments.Count - 1) {
                                if (flag.Type == typeof(string[])) {
                                    //multiple value (array)
                                    var list = new List<string>();
                                    if (flag.Default != null && value == flag.Default) {
                                    } else {
                                        if (value != null) list.AddRange((string[])value);
                                    }
                                    var aux = arguments[i + 1];
                                    if (aux.StartsWith(HYPHEN_PREFIX)) aux = aux.Substring(HYPHEN_PREFIX.Length);
                                    list.Add(aux);
                                    arguments.RemoveAt(i + 1);
                                    arguments.RemoveAt(i);
                                    i -= 1;
                                    value = list.ToArray();
                                    continue;
                                } else if (flag.Type == typeof(int[])) {
                                    //multiple value (array)
                                    var list = new List<int>();
                                    if (flag.Default != null && value == flag.Default) {
                                    } else {
                                        if (value != null) list.AddRange((int[])value);
                                    }
                                    var aux = arguments[i + 1];
                                    if (aux.StartsWith(HYPHEN_PREFIX)) aux = aux.Substring(HYPHEN_PREFIX.Length);
                                    list.Add(int.Parse(aux));
                                    arguments.RemoveAt(i + 1);
                                    arguments.RemoveAt(i);
                                    i -= 1;
                                    value = list.ToArray();
                                    continue;
                                } else {
                                    //single value
                                    var aux = arguments[i + 1];
                                    if (aux.StartsWith(HYPHEN_PREFIX)) aux = aux.Substring(HYPHEN_PREFIX.Length);
                                    value = aux;
                                    arguments.RemoveAt(i + 1);
                                    arguments.RemoveAt(i);
                                    break;
                                }
                            } else {
                                errors.Add(Name + ": flag --" + flagName + "=XXXX (-" + flag.Char + ") value required (" + flag.Description + ")");
                                break;
                            }
                        }
                    }
                    if (value == null) {
                        errors.Add(Name + ": flag --" + flagName + "=XXXX (-" + flag.Char + ") required (" + flag.Description + ")");
                    } else {
                        try {
                            var valueOfType = ConvertUtils.To(value, flag.Type, true, getService);
                            flag.PropertyInfo?.SetValue(instance, valueOfType);
                            dictionary?.Add(flag.Alias ?? flag.Name, value);
                        } catch (Exception e) {
                            errors.Add(Name + ": flag --" + flagName + " invalid: " + e.Message);
                        }
                    }
                }
                //arguments
                for (var i = 0; i < arguments.Count; i++) {
                    var argument = arguments[i];
                    if (argument.Equals(sheBangArgsSeparator)) {
                        break;
                    } else if (argument.StartsWith("-") && this.Remaining is null) {
                        errors.Add(Name + ": flag " + argument + " is invalid");
                    }
                }
                if (sheBangArgsSeparator != null) {
                    arguments.Remove(sheBangArgsSeparator);
                }
                int counter = 0;
                for (int i = 0; true; i++) {
                    var propertyFound = false;
                    foreach (var argument in this.Arguments) {
                        if (argument.Index == counter) {
                            propertyFound = true;
                            var value = argument.Default;
                            if (defaults != null) {
                                foreach (var def in defaults) {
                                    if (def.Key.Equals(argument.Name, StringComparison.OrdinalIgnoreCase)) {
                                        value = def.Value;
                                    }
                                }
                            }
                            if (argument.Type == typeof(string[])) {
                                if (arguments.Count == 0) {
                                    if (value != null) {
                                        value = ConvertUtils.To<string[]>(value);
                                    } else {
                                        value = null;
                                    }
                                } else {
                                    var valueAsList = new List<string>();
                                    for (var j = 0; j < arguments.Count; j++) {
                                        var arg = arguments[j];
                                        if (arg.StartsWith(HYPHEN_PREFIX)) arg = arg.Substring(HYPHEN_PREFIX.Length);
                                        valueAsList.Add(arg);
                                    }
                                    value = valueAsList.ToArray();
                                    arguments.Clear();
                                }
                            } else if (argument.Type == typeof(int[])) {
                                var valueAsList = new List<int>();
                                while (arguments.Count > 0 && int.TryParse(arguments[0], out int arg)) {
                                    valueAsList.Add(arg);
                                    arguments.RemoveAt(0);
                                }
                                value = valueAsList.ToArray();
                                if (valueAsList.Count == 0) {
                                    if (value != null) {
                                        value = ConvertUtils.To<int[]>(value);
                                    } else {
                                        value = null;
                                    }
                                }
                            } else if (argument.Type == typeof(object[])) {
                                if (arguments.Count == 0) {
                                    if (value != null) {
                                        value = ConvertUtils.To<object[]>(value);
                                    } else {
                                        value = null;
                                    }
                                } else {
                                    var valueAsList = new List<object?>();
                                    for (var j = 0; j < arguments.Count; j++) {
                                        var arg = arguments[j];
                                        if (arg.StartsWith(HYPHEN_PREFIX)) arg = arg.Substring(HYPHEN_PREFIX.Length);
                                        var argTypes = StringUtils.InferDataType(arg);
                                        if (argTypes.Length > 0) {
                                            var argObject = ConvertUtils.To(arg, argTypes[0], false, getService);
                                            valueAsList.Add(argObject);
                                        } else {
                                            valueAsList.Add(arg);
                                        }
                                    }
                                    value = valueAsList.ToArray();
                                    arguments.Clear();
                                }
                            } else {
                                if (arguments.Count > 0) {
                                    var arg = arguments[0];
                                    if (arg.StartsWith(HYPHEN_PREFIX)) arg = arg.Substring(HYPHEN_PREFIX.Length);
                                    value = arg;
                                    arguments.RemoveAt(0);
                                }
                            }
                            if (value == null) {
                                errors.Add(Name + ": argument '" + argument.Name.ToLower() + "' required");
                            } else {
                                try {
                                    var valueOfType = ConvertUtils.To(value, argument.Type, true, getService);
                                    argument.PropertyInfo?.SetValue(instance, valueOfType);
                                    dictionary?.Add(argument.Name, value);
                                } catch (Exception e) {
                                    errors.Add(Name + ": argument '" + argument.Name.ToLower() + "' invalid: " + e.Message);
                                }
                            }
                            counter++;
                            break;
                        }
                    }
                    if (!propertyFound) break;
                }
                //remaining
                if (Remaining != null) {
                    if (Remaining.Type == typeof(string[])) {
                        if (arguments.Count == 0 && Remaining.Default == null) {
                            errors.Add(Name + ": " + Remaining.Description + ": required");
                        } else {
                            var value = new List<string>();
                            for (var i = 0; i < arguments.Count; i++) {
                                var argument = arguments[i];
                                if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                                value.Add(argument);
                            }
                            Remaining.PropertyInfo?.SetValue(instance, value.ToArray());
                            arguments.Clear();
                        }
                    } else {
                        var value = new StringBuilder();
                        if (arguments.Count == 0) value.Append(Remaining.Default);
                        for (var i = 0; i < arguments.Count; i++) {
                            var argument = arguments[i];
                            if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                            if (value.Length > 0) value.Append(" ");
                            value.Append(argument);
                        }
                        Remaining.PropertyInfo?.SetValue(instance, value.ToString());
                        arguments.Clear();
                    }
                }                
            }
            //show error for each argument not assigned to a property
            for (var i = 0; i < arguments.Count; i++) {
                var argument = arguments[i];
                if (argument.StartsWith(HYPHEN_PREFIX)) argument = argument.Substring(HYPHEN_PREFIX.Length);
                errors.Add(Name + ": argument '" + argument + "' is not expected");
            }
            //return
            return (errors.Count == 0);
        }

    }

}
