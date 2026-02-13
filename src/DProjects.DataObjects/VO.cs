
using System;
using System.Collections;
using System.Collections.Generic;

using DProjects.Utils;


namespace DProjects.DataObjects {

    //[JsonConverter(typeof(DProjects.Core.Serialization.JsonConverters.VOConverter))]
    public class VO : Dictionary<string, object?> {


        //constructor
        public VO() {
        }
        public VO(IDictionary<string, object?> attributes) {
            foreach (var item in attributes) {
                this.Add(item.Key, item.Value);
            }
        }

        //properties
        public new object? this[string name] {
            get {
                if (TryGetValue(name, out object? value)) {
                    return value;
                }
                if (GetType() != typeof(VO)) {
                    var propertyInfo = this.GetType().GetProperty(name);
                    if (propertyInfo != null) {
                        return propertyInfo.GetValue(this);
                    }
                }
                return null;
            }
            set {
                base[name] = value;
            }
        }


        //methods
        public T? Get<T>(string name, T? defaultValue = default) {
            var value = this[name];
            if (value == null) return defaultValue;
            return ConvertUtils.To<T>(value);
        }
        public object? Get(string name, Type type, object? defaultValue = null) {
            var value = this[name];
            if (value == null) return defaultValue;
            return ConvertUtils.To(value, type, true);
        }
        public string ValueOf() {
            return ToString();
        }
        public object? Select(string path) {
            object? target = this;
            if (path.Length > 0) {
                var parts = path.Split(new char[] { '.', '[' });
                for (var i = 0; i < parts.Length; i++) {
                    var part = parts[i];
                    var last = (i == parts.Length - 1);
                    if (part.EndsWith("]")) {
                        //must be a list or array 
                        var index = int.Parse(part.Substring(0, part.Length - 1));
                        if (target is IList) {
                            var list = (IList)target;
                            if (last) {
                                return list[(index < 0 ? list.Count + index : index)];
                            } else {
                                target = list[(index < 0 ? list.Count + index : index)];
                            }
                        } else {
                            throw new Exception("Unable to query vo: object is not a list: " + path);
                        }
                    } else {
                        if (target is IDictionary<string, object?>) {
                            var dict = (IDictionary<string, object?>)target;
                            if (last) {
                                return dict[part];
                            } else {
                                target = dict[part];
                            }
                        } else {
                            throw new Exception("Unable to query vo: object is not a dictionary: " + path);
                        }
                    }
                }
            }
            return target;
        }
        public bool Modify(string path, object? value) {
            object? target = this;
            var parts = path.Split(new char[] { '.', '[' });
            for (var i = 0; i < parts.Length; i++) {
                var part = parts[i];
                var last = (i == parts.Length - 1);
                if (part.EndsWith("]")) {
                    //must be a list or array 
                    var index = int.Parse(part.Substring(0, part.Length - 1));
                    if (target is IList) {
                        var list = (IList)target;
                        if (last) {
                            list[(index < 0 ? list.Count + index : index)] = value;
                            return true;
                        } else {
                            target = list[(index < 0 ? list.Count + index : index)];
                        }
                    } else {
                        throw new Exception("Unable to modify vo: object is not a list: " + path);
                    }
                } else {
                    if (target is IDictionary<string, object?>) {
                        var dict = (IDictionary<string, object?>)target;
                        if (last) {
                            dict[part] = value;
                            return true;
                        } else {
                            target = dict[part];
                        }
                    } else {
                        throw new Exception("Unable to modify vo: object is not a dictionary: " + path);
                    }
                }
            }
            return false;
        }
        public bool Delete(string path) {
            object? target = this;
            var parts = path.Split(new char[] { '.', '[' });
            for (var i = 0; i < parts.Length; i++) {
                var part = parts[i];
                var last = (i == parts.Length - 1);
                if (part.EndsWith("]")) {
                    var index = int.Parse(part.Substring(0, part.Length - 1));
                    if (target is IList) {
                        var list = (IList)target;
                        if (last) {
                            if (ArrayUtils.IsArray(list)) {
                                var aux = new List<object?>();
                                for (var k = 0; k < list.Count; k++) {
                                    if (k != (index < 0 ? list.Count + index : index)) aux.Add(list[k]);
                                }
                                var pathParent = path.Substring(0, path.LastIndexOf("["));
                                Modify(pathParent, aux);
                            } else {
                                list.RemoveAt((index < 0 ? list.Count + index : index));
                            }
                            return true;
                        } else {
                            target = list[(index < 0 ? list.Count + index : index)];
                        }
                    } else {
                        throw new Exception("Unable to remove vo: object is not a list: " + path);
                    }
                } else {
                    if (target is IDictionary<string, object?>) {
                        var dict = (IDictionary<string, object?>)target;
                        if (last) {
                            dict.Remove(part);
                            return true;
                        } else {
                            target = dict[part];
                        }
                    } else {
                        throw new Exception("Unable to remove vo: object is not a dictionary: " + path);
                    }
                }
            }
            return false;
        }
        public void Import(VO vo2) {
            foreach (var key in vo2.Keys) {
                var value2 = vo2[key];
                if (this.ContainsKey(key)) {
                    var value1 = this[key];
                    if (value1 == null) {
                        this[key] = value2;
                    } else if (value1 is IList || value2 is IList) {
                        var list = new List<object>();
                        var list1 = (IList)(value1 ?? new object[] { });
                        var list2 = (IList)(value2 ?? new object[] { });
                        foreach (var item1 in list1) {
                            list.Add(item1);
                        }
                        foreach (var item2 in list2) {
                            if (!list.Contains(item2)) list.Add(item2);
                        }
                        this[key] = list.ToArray();
                    } else if (value1 is VO && value2 is VO) {
                        ((VO)value1).Import((VO)value2);
                    } else {
                        this[key] = value2;
                    }
                } else {
                    this[key] = value2;
                }
            }
        }
        public VO Clone() {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Json.JsonSerializer.Deserialize<VO>(json)!;
            //var json = DProjects.Core.Serialization.JsonSerializer.Serialize(this);
            //return DProjects.Core.Serialization.JsonDeserializer.Deserialize<VO>(json);
        }
    }


}
