using System.Collections;
using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateObjectAccessTests {

        // methods
        [Fact]
        public void ExposesStringKeyDictionariesForSpreadsAndIteration() {
            var state = new Dictionary<string, object?> {
                ["attributes"] = new Dictionary<string, object?> { ["title"] = "Hello", ["hidden"] = false },
                ["values"] = new Dictionary<string, object?> { ["beta"] = 2, ["alpha"] = 1 }
            };

            var renderer = new XTemplateRenderer();
            var template = "<div x-attr=\"state.attributes\"></div><i x-for=\"key in state.values\">{{ key }}</i>";
            var html = renderer.Render(template, state);

            Assert.Equal("<div title=\"Hello\"></div><i>beta</i><i>alpha</i>", html);
            Assert.Equal(html, renderer.Render(template, state));
        }

        [Fact]
        public void DoesNotReflectOrInvokePocoGettersByDefault() {
            var model = new SideEffectModel();
            var state = new Dictionary<string, object?> { ["model"] = model };
            var renderer = new XTemplateRenderer();

            Assert.Null(XTemplateExpressions.Evaluate("state.model.Dangerous", new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state })));
            var exception = Assert.Throws<XTemplateException>(() => renderer.Render("<i x-for=\"key in state.model\">{{ key }}</i>", state));

            Assert.Contains("not exposed to XTemplate", exception.Message, StringComparison.Ordinal);
            Assert.Equal(0, model.GetterCalls);
        }

        [Fact]
        public void UsesExplicitAdaptersForMembersAndObjectValuedAttributes() {
            var state = new Dictionary<string, object?> { ["model"] = new AttributeModel() };
            var renderer = new XTemplateRenderer(new[] { new AttributeModelAdapter() });

            var html = renderer.Render("<div x-attr:class=\"state.model\"></div><i x-for=\"key in state.model\">{{ key }}</i>", state);

            Assert.Equal("<div class=\"active featured\"></div><i>active</i><i>featured</i>", html);
        }

        [Fact]
        public void RejectsNonStringDictionaryKeys() {
            var state = new Dictionary<string, object?> { ["values"] = new Hashtable { [1] = "one" } };

            var exception = Assert.Throws<XTemplateException>(() => new XTemplateRenderer().Render("<i x-for=\"key in state.values\">{{ key }}</i>", state));

            Assert.Contains("must use string keys", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void UsesReadOnlyDictionaryMembersBeforeCollectionSemantics() {
            IReadOnlyDictionary<string, object?> map = new ReadOnlyMap(new Dictionary<string, object?> { ["name"] = "Ada" });
            var state = new Dictionary<string, object?> { ["map"] = map };
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state });

            Assert.Equal("Ada", XTemplateExpressions.Evaluate("state.map.name", context));
            Assert.Null(XTemplateExpressions.Evaluate("state.map.missing", context));
        }

        [Fact]
        public void UsesExplicitAdapterBeforeCollectionSemanticsForEnumerableObjects() {
            var state = new Dictionary<string, object?> { ["model"] = new AdaptedEnumerable() };
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state }, new[] { new AdaptedEnumerableAdapter() });

            Assert.Equal("adapted", XTemplateExpressions.Evaluate("state.model.name", context));
            Assert.Equal("adapter-length", XTemplateExpressions.Evaluate("state.model.length", context));
            Assert.Equal("adapted", XTemplateExpressions.Evaluate("state.model[\"name\"]", context));
        }

        [Fact]
        public void KeepsCollectionSemanticsForUnadaptedLists() {
            var state = new Dictionary<string, object?> { ["items"] = new[] { "first", "second" } };
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state });

            Assert.Equal(2d, XTemplateExpressions.Evaluate("state.items.length", context));
            Assert.Equal("second", XTemplateExpressions.Evaluate("state.items[1]", context));
        }

        // methods (private)
        private sealed class SideEffectModel {

            // vars
            private int _getterCalls;

            // props
            public int GetterCalls => _getterCalls;
            public string Dangerous { get { _getterCalls++; return "unsafe"; } }
        }
        private sealed class AttributeModel { }
        private sealed class AttributeModelAdapter : IXTemplateObjectAdapter {

            // methods
            public bool CanAdapt(object value) => value is AttributeModel;
            public bool TryGetMember(object value, string name, out object? member) {
                member = name switch { "active" => true, "featured" => true, _ => null };
                return member != null;
            }
            public IEnumerable<KeyValuePair<string, object?>> GetMembers(object value) {
                yield return new("active", true);
                yield return new("featured", true);
            }
        }

        private sealed class ReadOnlyMap : IReadOnlyDictionary<string, object?> {

            // vars
            private readonly Dictionary<string, object?> _values;

            // props
            public object? this[string key] => _values[key];
            public IEnumerable<string> Keys => _values.Keys;
            public IEnumerable<object?> Values => _values.Values;
            public int Count => _values.Count;

            // ctor
            public ReadOnlyMap(Dictionary<string, object?> values) {
                _values = values;
            }

            // methods
            public bool ContainsKey(string key) => _values.ContainsKey(key);
            public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);
            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class AdaptedEnumerable : IEnumerable {

            // methods
            public IEnumerator GetEnumerator() => new[] { "collection-value" }.GetEnumerator();
        }

        private sealed class AdaptedEnumerableAdapter : IXTemplateObjectAdapter {

            // methods
            public bool CanAdapt(object value) => value is AdaptedEnumerable;
            public bool TryGetMember(object value, string name, out object? member) {
                member = name switch { "name" => "adapted", "length" => "adapter-length", _ => null };
                return member != null;
            }
            public IEnumerable<KeyValuePair<string, object?>> GetMembers(object value) {
                yield return new("name", "adapted");
                yield return new("length", "adapter-length");
            }
        }
    }
}
