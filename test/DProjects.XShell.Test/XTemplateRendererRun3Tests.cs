using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateRendererRun3Tests {

        // methods
        [Fact]
        public void RendersRecursiveItemsWithDepthFirstScopeAndWrapper() {
            var state = new {
                items = new[] {
                    new Node("Root", new[] { new Node("Leaf", Array.Empty<Node>()) }),
                    new Node("Other", Array.Empty<Node>())
                }
            };
            var html = Render("<ul><li x-recursive=\"(item,index,indexAbsolute) in state.items\" x-recursive-wrapper=\"ul\">{{ indent }}:{{ index }}:{{ indexAbsolute }}:{{ item.name }}</li></ul>", state);

            Assert.Equal("<ul><li>0:0:0:Root<ul><li>1:0:1:Leaf</li></ul></li><li>0:1:2:Other</li></ul>", html);
        }

        [Fact]
        public void RendersNoRecursiveChildrenForNullOrEmptyCollections() {
            var html = Render("<li x-recursive=\"item in state.items\">{{ item.name }}</li>", new { items = new[] { new { name = "One", children = (object?)null }, new { name = "Two", children = (object?)Array.Empty<object>() } } });

            Assert.Equal("<li>One</li><li>Two</li>", html);
        }

        [Fact]
        public void ValidatesRecursiveSyntaxWrapperAndKeyUse() {
            Assert.Throws<XTemplateException>(() => Render("<li x-recursive=\"(item,index,absolute,extra) in state.items\"></li>", new { items = Array.Empty<object>() }));
            Assert.Throws<XTemplateException>(() => Render("<li x-recursive-wrapper=\"ul\"></li>"));
            Assert.Throws<XTemplateException>(() => Render("<li x-key=\"id\"></li>"));
            Assert.Equal("<li>A</li>", Render("<li x-recursive=\"item in state.items\" x-key=\"id\">{{ item.name }}</li>", new { items = new[] { new { id = 1, name = "A", children = Array.Empty<object>() } } }));
        }

        [Fact]
        public void SerializesInputModelValuesWithoutClientBehavior() {
            var state = new { name = "M<&\"'", count = 4, enabled = true, disabled = false, choice = "b" };
            var html = Render("<input x-model=\"state.name\"><input type=\"number\" x-model=\"state.count\"><input type=\"checkbox\" x-model=\"state.enabled\"><input type=\"checkbox\" x-model=\"state.disabled\"><input type=\"radio\" value=\"a\" x-model=\"state.choice\"><input type=\"radio\" value=\"b\" x-model=\"state.choice\">", state);

            Assert.Equal("<input value=\"M&lt;&amp;&quot;&#39;\"><input type=\"number\" value=\"4\"><input type=\"checkbox\" checked><input type=\"checkbox\"><input type=\"radio\" value=\"a\"><input type=\"radio\" value=\"b\" checked>", html);
        }

        [Fact]
        public void RejectsInvalidAndAmbiguousModelCases() {
            Assert.Throws<XTemplateException>(() => Render("<input x-model=\"state.a + state.b\">", new { a = 1, b = 2 }));
            Assert.Throws<XTemplateException>(() => Render("<select multiple x-model=\"state.value\"><option value=\"a\">A</option></select>", new { value = "a" }));
        }

        [Theory]
        [InlineData("a", "<select><option value=\"a\" selected>A</option><option value=\"b\">B</option><option value=\"c\">C</option></select>")]
        [InlineData("b", "<select><option value=\"a\">A</option><option value=\"b\" selected>B</option><option value=\"c\">C</option></select>")]
        [InlineData("c", "<select><option value=\"a\">A</option><option value=\"b\">B</option><option value=\"c\" selected>C</option></select>")]
        public void SelectModelMarksTheMatchingExplicitValueOption(string value, string expected) {
            var html = Render("<select x-model=\"state.value\"><option value=\"a\">A</option><option value=\"b\">B</option><option value=\"c\">C</option></select>", new { value });

            Assert.Equal(expected, html);
        }

        [Fact]
        public void SelectModelNormalizesOptionsAndPreservesOptionMarkup() {
            var html = Render("<select id=\"choices\" x-model=\"state.value\"><option value=\"1\" selected data-id=\"first\">One</option><option class=\"choice\" selected data-id=\"second\"><span>Two</span> &amp; more</option><option value=\"3\" selected>Three</option></select>", new { value = "Two & more" });

            Assert.Equal("<select id=\"choices\"><option value=\"1\" data-id=\"first\">One</option><option class=\"choice\" selected data-id=\"second\"><span>Two</span> &amp; more</option><option value=\"3\">Three</option></select>", html);
        }

        [Fact]
        public void SelectModelUsesScalarConversionAndLeavesNoMatchUnselected() {
            Assert.Equal("<select><option value=\"1\">One</option><option value=\"2\" selected>Two</option></select>", Render("<select x-model=\"state.value\"><option value=\"1\">One</option><option value=\"2\">Two</option></select>", new { value = 2 }));
            Assert.Equal("<select><option value=\"a\">A</option><option value=\"b\">B</option></select>", Render("<select x-model=\"state.value\"><option value=\"a\" selected>A</option><option value=\"b\">B</option></select>", new { value = "missing" }));
        }

        [Fact]
        public void PreservesPreformattedChildMarkupAndEvaluatesOutsideIt() {
            var html = Render("<div x-pre>{{ state.name }}<span x-if=\"state.visible\">Test</span><!-- note --></div><p>{{ state.name }}</p>", new { name = "Ada", visible = true });

            Assert.Equal("<div>{{ state.name }}<span x-if=\"state.visible\">Test</span><!-- note --></div><p>Ada</p>", html);
        }

        [Fact]
        public void RejectsBrowserDomChildrenButSuppressesOtherBrowserDirectives() {
            Assert.Throws<XTemplateException>(() => Render("<div x-children=\"state.node\"></div>", new { node = new object() }));
            Assert.Equal("<button>Save</button>", Render("<button x-on:click.stop=\"save\" x-prop:value=\"state.value\" x-once>Save</button>", new { value = "x" }));
        }

        [Theory]
        [InlineData("<button x-on:=\"save\"></button>")]
        [InlineData("<button x-on:click=\"\"></button>")]
        [InlineData("<div x-prop=\"state.value\"></div>")]
        public void RejectsMalformedBrowserOnlyDirectives(string template) {
            Assert.Throws<XTemplateException>(() => Render(template, new { value = "x" }));
        }

        [Fact]
        public void CombinesRecursiveBindingsWithoutDirectiveLeakage() {
            var state = new { visible = true, selectedId = 2, menu = new[] { new Menu(1, "One", "/one", new[] { new Menu(2, "Two", "/two", Array.Empty<Menu>()) }) } };
            var html = Render("<nav x-if=\"state.visible\"><ul><li x-recursive=\"(item,index,indexAbsolute) in state.menu\" x-recursive-wrapper=\"ul\" x-key=\"id\" x-class:selected=\"item.id == state.selectedId\"><a x-attr:href=\"item.href\">{{ item.label }}</a></li></ul></nav>", state);

            Assert.Equal("<nav><ul><li><a href=\"/one\">One</a><ul><li class=\"selected\"><a href=\"/two\">Two</a></li></ul></li></ul></nav>", html);
            Assert.DoesNotContain("x-", html, StringComparison.Ordinal);
        }

        // methods (private)
        private static string Render(string template, object? state = null) => new XTemplateRenderer().Render(template, state ?? new { });
        private sealed record Node(string name, Node[] children);
        private sealed record Menu(int id, string label, string href, Menu[] children);
    }
}
