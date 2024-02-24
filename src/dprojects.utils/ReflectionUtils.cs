using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DProjects.Utils {


    public static class ReflectionUtils {


        //fields
        public static void SetObjectFieldValue(object aObject, string fieldName, object value) {
            Type type = aObject.GetType();
            FieldInfo? fieldInfo = type.GetField(fieldName, (BindingFlags)(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (fieldInfo == null) throw new Exception("Field \'" + fieldName + "\' not exists in object " + aObject.ToString());
            fieldInfo.SetValue(aObject, value);
        }
        public static void SetObjectFieldValue(object aObject, string fieldName, object value, bool autoconvert) {
            Type type = aObject.GetType();
            FieldInfo? fieldInfo = type.GetField(fieldName, (BindingFlags)(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (fieldInfo == null) throw new Exception("Field \'" + fieldName + "\' not exists in object " + aObject.ToString());
            if (!autoconvert) {
                fieldInfo.SetValue(aObject, value);
            } else {
                fieldInfo.SetValue(aObject, ConvertUtils.To(value, fieldInfo.FieldType, false));
            }
        }
        public static object? GetObjectFieldValue(object aObject, string fieldName) {
            Type type = aObject.GetType();
            FieldInfo? fieldInfo = type.GetField(fieldName, (BindingFlags)(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (fieldInfo == null) throw new Exception("Field \'" + fieldName + "\' not exists in object " + aObject.ToString());
            return fieldInfo.GetValue(aObject);
        }


        //properties
        public static object? GetObjectPropertyValue(object aObject, string propertyName, object[]? args = null) {
            Type type = aObject.GetType();
            PropertyInfo? propertyInfo = type.GetProperty(propertyName, (BindingFlags)(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (propertyInfo == null) throw new Exception("Property \'" + propertyName + "\' not exists in object " + aObject.ToString());
            if (args == null) args = new object[] { };
            return propertyInfo.GetValue(aObject, args);
        }
        public static object? GetObjectPropertyValueTryingDefaultProperty(object aObject, string propertyName, object[]? args = null) {
            Type type = aObject.GetType();
            PropertyInfo? propertyInfo = type.GetProperty(propertyName, (BindingFlags)(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (propertyInfo == null) {
                foreach (MemberInfo objMemberInfo in type.GetDefaultMembers()) {
                    if (objMemberInfo is PropertyInfo) {
                        propertyInfo = (PropertyInfo)objMemberInfo;
                        args = new string[] { propertyName };
                        break;
                    }
                }
            }
            if (propertyInfo == null) throw new Exception("Unablet to get property: not found: " + propertyName + ", " + aObject.ToString());
            args ??= new object[] { };
            return propertyInfo.GetValue(aObject, args);
        }
        public static void SetObjectPropertyValue(object aObject, string propertyName, object value) {
            Type type = aObject.GetType();
            PropertyInfo? propertyInfo = type.GetProperty(propertyName, (BindingFlags)(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (propertyInfo == null) throw new Exception("Unable to set property: not found: " + propertyName + ", " + aObject.ToString());
            propertyInfo.SetValue(aObject, value, new object[] { });
        }
        public static void SetObjectPropertyValue(object aObject, string propertyName, object value, bool autoconvert) {
            Type type = aObject.GetType();
            PropertyInfo? propertyInfo = type.GetProperty(propertyName, (BindingFlags)(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (propertyInfo == null) throw new Exception("Unable to set property: not found: " + propertyName + ", " + aObject.ToString());
            if (!autoconvert) {
                propertyInfo.SetValue(aObject, value, new object[] { });
            } else {
                propertyInfo.SetValue(aObject, ConvertUtils.To(value, propertyInfo.PropertyType, false), new object[] { });
            }
        }


        //methods
        public static object? CallObjectMethod(object aObject, string methodName, object[] args) {
            Type type = aObject.GetType();
            var methodInfo = type.GetMethod(methodName, (BindingFlags)(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
            if (methodInfo == null) {
                methodInfo = type.GetMethod(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (methodInfo == null) throw new Exception("Method \'" + methodName + "\' not found in object " + aObject.ToString());
            }
            return methodInfo.Invoke(aObject, args);
        }
        public static object? CallObjectMethod(object aObject, string methodName, object?[] args, bool autoConvertArguments) {
            if (!autoConvertArguments) {
                Type type = aObject.GetType();
                MethodInfo? methodInfo = type.GetMethod(methodName, (BindingFlags)(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
                if (methodInfo == null) {
                    methodInfo = type.GetMethod(methodName, (BindingFlags)(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Static | BindingFlags.IgnoreCase));
                    if (methodInfo == null) throw new Exception("Method \'" + methodName + "\' not found in object " + aObject.ToString());
                }
                return methodInfo.Invoke(aObject, args);
            } else {
                Type type = aObject.GetType();
                MethodInfo? methodInfo = type.GetMethod(methodName, (BindingFlags)(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase));
                if (methodInfo == null) throw new Exception("Method \'" + methodName + "\' not found in object " + aObject.ToString());
                object?[]? arguments = null;
                int iMessageArgumentsIndex = 0;
                int iMethodArgumentsIndex = 0;
                ParameterInfo[] objMethodParameters = methodInfo.GetParameters();
                arguments = new object[objMethodParameters.Length];
                foreach (ParameterInfo parameterInfo in objMethodParameters) {
                    if (args.Length <= iMessageArgumentsIndex) {
                        if (parameterInfo.ParameterType == typeof(bool)) {
                            Array.Resize(ref args, iMessageArgumentsIndex + 1);
                            args[iMessageArgumentsIndex - 1] = false;
                        } else {
                            throw new ArgumentException("Error, argument \'" + parameterInfo.Name + "\' not specified.");
                        }
                    }
                    object? candidateParameter = args[iMessageArgumentsIndex];
                    if (candidateParameter != null) {
                        if (candidateParameter.GetType() != parameterInfo.ParameterType && !candidateParameter.GetType().GetTypeInfo().IsSubclassOf(parameterInfo.ParameterType)) {
                            if (parameterInfo.ParameterType == typeof(DateTime)) {
                                if (candidateParameter is string && (candidateParameter).ToString() == "") {
                                    candidateParameter = default(DateTime);
                                } else {
                                    candidateParameter = Convert.ToDateTime(candidateParameter);
                                }
                            } else if (parameterInfo.ParameterType == typeof(int)) {
                                if (candidateParameter.GetType() == typeof(string) && (candidateParameter).ToString() == "") {
                                    candidateParameter = 0;
                                } else {
                                    candidateParameter = Convert.ToInt32(candidateParameter);
                                }
                            } else if (parameterInfo.ParameterType == typeof(long)) {
                                candidateParameter = Convert.ToInt64(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(bool)) {
                                candidateParameter = Convert.ToBoolean(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(double)) {
                                candidateParameter = Convert.ToDouble(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(byte)) {
                                candidateParameter = Convert.ToByte(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(char)) {
                                candidateParameter = Convert.ToChar(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(decimal)) {
                                candidateParameter = Convert.ToDecimal(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(string)) {
                                candidateParameter = Convert.ToString(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(Single)) {
                                candidateParameter = Convert.ToSingle(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(byte[])) {
                                candidateParameter = Base64Utils.FromBase64(candidateParameter.ToString() ?? "");
                            } else if (parameterInfo.ParameterType == typeof(string[])) {
                                if (candidateParameter is System.Array) {
                                    var o = new List<string>();
                                    foreach (object? oo in ((System.Array)candidateParameter)) {
                                        if (oo is null) {
                                            o.Add("");
                                        } else {
                                            o.Add(oo.ToString() ?? "");
                                        }
                                    }
                                    candidateParameter = o.ToArray();
                                }
                            } else if (parameterInfo.ParameterType == typeof(int[])) {
                                if (candidateParameter is System.Array) {
                                    List<int> o = new List<int>();
                                    foreach (object? oo in ((System.Array)candidateParameter)) {
                                        if (oo != null) o.Add(Convert.ToInt32(oo));
                                    }
                                    candidateParameter = o.ToArray();
                                }
                            } else if (parameterInfo.ParameterType.GetTypeInfo().IsEnum) {
                                candidateParameter = Convert.ToInt32(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(Dictionary<string, string>)) {
                                if (candidateParameter.GetType() == typeof(object[]) && ((object[])candidateParameter).Length == 0) {
                                    candidateParameter = new Dictionary<string, string>();
                                } else if (candidateParameter.GetType() == typeof(Dictionary<string, object>)) {
                                    var c = (Dictionary<string, object>)candidateParameter;
                                    var c2 = new Dictionary<string, string>();
                                    foreach (string key in c.Keys) {
                                        object v = c[key];
                                        if (v == null) {
                                            c2[key] = "";
                                        } else {
                                            c2[key] = v.ToString() ?? "";
                                        }
                                    }
                                    candidateParameter = c2;
                                }
                            } else {
                                throw new ArgumentException("Unable to convert argument \'" + parameterInfo.Name + "\' from type \'" + candidateParameter.GetType().Name + "\' to type \'" + parameterInfo.ParameterType.Name + "\'.");
                            }
                        }
                    }
                    arguments[iMethodArgumentsIndex] = candidateParameter;
                    iMessageArgumentsIndex++;
                    iMethodArgumentsIndex++;
                }
                return methodInfo.Invoke(aObject, arguments);
            }
        }
        public static object? CallObjectMethod(object aObject, string methodName, Dictionary<string, object> args) {
            Type type = aObject.GetType();
            MethodInfo? methodInfo = type.GetMethod(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (methodInfo == null) throw new Exception("Method \'" + methodName + "\' not found in object " + aObject.ToString());
            var arguments = new List<object>();
            foreach (ParameterInfo parameterInfo in methodInfo.GetParameters()) {
                object? candidateParameter = null;
                foreach (string argName in args.Keys) {
                    if (argName.ToLower().Equals(parameterInfo.Name, StringComparison.OrdinalIgnoreCase)) {
                        candidateParameter = args[argName];
                        if (candidateParameter.GetType() != parameterInfo.ParameterType && !candidateParameter.GetType().GetTypeInfo().IsSubclassOf(parameterInfo.ParameterType)) {
                            if (parameterInfo.ParameterType == typeof(DateTime)) {
                                candidateParameter = Convert.ToDateTime(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(int)) {
                                candidateParameter = Convert.ToInt32(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(long)) {
                                candidateParameter = Convert.ToInt64(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(bool)) {
                                candidateParameter = Convert.ToBoolean(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(double)) {
                                candidateParameter = Convert.ToDouble(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(byte)) {
                                candidateParameter = Convert.ToByte(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(char)) {
                                candidateParameter = Convert.ToChar(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(decimal)) {
                                candidateParameter = Convert.ToDecimal(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(string)) {
                                candidateParameter = Convert.ToString(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(Single)) {
                                candidateParameter = Convert.ToSingle(candidateParameter);
                            } else if (parameterInfo.ParameterType == typeof(byte[])) {
                                candidateParameter = Base64Utils.FromBase64(candidateParameter.ToString() ?? "");
                            } else {
                                throw new ArgumentException("Unable to convert argument \'" + parameterInfo.Name + "\' from type \'" + candidateParameter.GetType().Name + "\' to type \'" + parameterInfo.ParameterType.Name + "\'.");
                            }
                        }
                    }
                }
                if (candidateParameter == null) {
                    throw new Exception("Agument \'" + parameterInfo.Name + "\' not especified");
                }
                arguments.Add(candidateParameter);
            }
            return methodInfo.Invoke(aObject, arguments.ToArray());
        }


        //AssemblyLoadContext
        //private class MyAssemblyLoadContext {
        //    public AssemblyLoadContext? AssemblyLoadContext;
        //    public System.Reflection.Assembly? EntryAssembly;
        //    public MyAssemblyLoadContext(System.Reflection.Assembly? entryAssembly) {
        //        if (entryAssembly != null) {
        //            AssemblyLoadContext = AssemblyLoadContext.GetLoadContext(entryAssembly);
        //            EntryAssembly = entryAssembly;
        //        }
        //    }
        //}
        //private static List<MyAssemblyLoadContext> mMyAssemblyLoadContexts;
        //static ReflectionUtils() {
        //    mMyAssemblyLoadContexts = new List<MyAssemblyLoadContext>();
        //    mMyAssemblyLoadContexts.Add(new MyAssemblyLoadContext(System.Reflection.Assembly.GetEntryAssembly()));
        //}
        //public static void RegisterAssemblyLoadContextByAssembly(System.Reflection.Assembly entryAssembly) {
        //    mMyAssemblyLoadContexts.Add(new MyAssemblyLoadContext(entryAssembly));
        //}
        //public static void UnregisterAssemblyLoadContextByAssembly(System.Reflection.Assembly entryAssembly) {
        //    for (var i = 0; i < mMyAssemblyLoadContexts.Count; i++) {
        //        var myAssemblyLoadContext = mMyAssemblyLoadContexts[i];
        //        if (myAssemblyLoadContext.EntryAssembly != null) {
        //            if (myAssemblyLoadContext.EntryAssembly.GetName().Equals(entryAssembly.GetName())) {
        //                mMyAssemblyLoadContexts.RemoveAt(i);
        //                break;
        //            }
        //        }
        //    }
        //}
        //public static System.Reflection.Assembly? GetAssemblyByName(string assemblyName) {
        //    //ex: System.Data.SqlClient
        //    //ex: System.Data.SqlClient, version=4.5.1
        //    return GetAssemblyByName(new AssemblyName(assemblyName));
        //}
        //public static System.Reflection.Assembly? GetAssemblyByName(AssemblyName assemblyName) {
        //    //ex: System.Data.SqlClient
        //    //ex: System.Data.SqlClient, version=4.5.1
        //    foreach (var alc in mMyAssemblyLoadContexts) {
        //        try {
        //            if (alc.AssemblyLoadContext != null) {
        //                return alc.AssemblyLoadContext.LoadFromAssemblyName(assemblyName);
        //            }
        //        } catch (FileNotFoundException) {
        //        }
        //    }
        //    return null;
        //}
        //public static Type? GetTypeByName(string typeName, bool ignoreCase = false) {
        //    if (typeName.IndexOf(",") != -1) {
        //        var assembly = GetAssemblyByName(typeName.Substring(typeName.IndexOf(",") + 1).Trim());
        //        if (assembly != null) {
        //            var aux = typeName.Substring(0, typeName.IndexOf(",")).Trim();
        //            return assembly.GetType(aux, false, ignoreCase);
        //        }
        //    } else {
        //        var type = Type.GetType(typeName, false, ignoreCase);
        //        if (type != null) return type;
        //        foreach (var alc in mMyAssemblyLoadContexts) {
        //            if (alc.EntryAssembly != null) {
        //                type = alc.EntryAssembly.GetType(typeName, false, ignoreCase);
        //                if (type != null) return type;
        //                foreach (var assemblyName in alc.EntryAssembly.GetReferencedAssemblies()) {
        //                    var assembly = System.Reflection.Assembly.Load(assemblyName);
        //                    type = assembly.GetType(typeName, false, ignoreCase);
        //                    if (type != null) return type;
        //                };
        //            }
        //        }
        //    }
        //    return null;
        //}

        //check
        public static bool GetTypeIsNumeric(Type t) {
            switch (Type.GetTypeCode(t)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        public static bool GetTypeIsSubclassOfRawGeneric(Type generic, Type toCheck) {
            //ex: GetTypeIsSubclassOfRawGeneric(typeof(List<>), aTargetType)
            while (toCheck != null && toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

    }


}


