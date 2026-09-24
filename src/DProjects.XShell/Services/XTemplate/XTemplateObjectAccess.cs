using System.Collections;
using System.Reflection;

namespace DProjects.XShell.Services.XTemplate {

    public interface IXTemplateObjectAdapter {

        // methods
        bool CanAdapt(object value);
        bool TryGetMember(object value, string name, out object? member);
        IEnumerable<KeyValuePair<string, object?>> GetMembers(object value);
    }

    /// <summary>Explicitly exposes public instance properties and fields as XTemplate members.</summary>
    public sealed class XTemplateReflectionObjectAdapter : IXTemplateObjectAdapter {

        // methods
        public bool CanAdapt(object value) => value != null && value is not IEnumerable;
        public bool TryGetMember(object value, string name, out object? member) {
            var property = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property?.CanRead == true && property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true) {
                member = property.GetValue(value);
                return true;
            }
            var field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            if (field != null) {
                member = field.GetValue(value);
                return true;
            }
            member = null;
            return false;
        }
        public IEnumerable<KeyValuePair<string, object?>> GetMembers(object value) {
            foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true)
                         .OrderBy(property => property.Name, StringComparer.Ordinal)) yield return new(property.Name, property.GetValue(value));
            foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(field => field.Name, StringComparer.Ordinal)) yield return new(field.Name, field.GetValue(value));
        }
    }

    internal sealed class XTemplateObjectAccess {

        // vars
        private readonly IReadOnlyList<IXTemplateObjectAdapter> _adapters;

        // ctor
        public XTemplateObjectAccess(IEnumerable<IXTemplateObjectAdapter>? adapters = null) {
            _adapters = new[] { new DictionaryAdapter() }.Concat(adapters ?? Array.Empty<IXTemplateObjectAdapter>()).ToArray();
        }

        // props
        public IEnumerable<IXTemplateObjectAdapter> Adapters => _adapters.Skip(1);

        // methods
        public bool CanAdapt(object value) => _adapters.Any(adapter => adapter.CanAdapt(value));
        public bool TryGetMember(object value, string name, out object? member) {
            var adapter = _adapters.FirstOrDefault(adapter => adapter.CanAdapt(value));
            if (adapter == null) {
                member = null;
                return false;
            }
            return adapter.TryGetMember(value, name, out member);
        }
        public IEnumerable<KeyValuePair<string, object?>> GetMembers(object value) {
            var adapter = _adapters.FirstOrDefault(adapter => adapter.CanAdapt(value));
            if (adapter == null) throw new XTemplateObjectAccessException($"Type '{value.GetType().FullName}' is not exposed to XTemplate. Register an IXTemplateObjectAdapter to expose its members.");
            return adapter.GetMembers(value);
        }

        // methods (private)
        private sealed class DictionaryAdapter : IXTemplateObjectAdapter {

            // methods
            public bool CanAdapt(object value) => value is IDictionary or IReadOnlyDictionary<string, object?>;
            public bool TryGetMember(object value, string name, out object? member) {
                if (value is IDictionary dictionary) {
                    if (dictionary.Contains(name)) {
                        member = dictionary[name];
                        return true;
                    }
                    member = null;
                    return false;
                }
                return ((IReadOnlyDictionary<string, object?>)value).TryGetValue(name, out member);
            }
            public IEnumerable<KeyValuePair<string, object?>> GetMembers(object value) {
                if (value is IDictionary dictionary) {
                    foreach (DictionaryEntry entry in dictionary) {
                        if (entry.Key is not string key) throw new XTemplateObjectAccessException("XTemplate object dictionaries must use string keys.");
                        yield return new(key, entry.Value);
                    }
                    yield break;
                }
                foreach (var entry in (IReadOnlyDictionary<string, object?>)value) yield return entry;
            }
        }
    }

    internal sealed class XTemplateObjectAccessException : Exception {

        // ctor
        public XTemplateObjectAccessException(string message) : base(message) { }
    }
}
