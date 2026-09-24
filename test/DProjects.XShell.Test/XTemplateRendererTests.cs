using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateRendererTests {

        // methods
        [Fact]
        public void RendersStaticHtmlCommentsCustomAndVoidElements() {
            var html = Render("<!-- note --><x-button title=\"A &amp; B\"></x-button><br><img src=\"a.png\">");

            Assert.Equal("<!-- note --><x-button title=\"A &amp; B\"></x-button><br><img src=\"a.png\">", html);
        }

        [Theory]
        [InlineData("   <div>A</div>   ", "<div>A</div>")]
        [InlineData("\n\t<div>A</div>\r\n", "<div>A</div>")]
        [InlineData("<div>  A  </div>", "<div>  A  </div>")]
        [InlineData("\n<div>A</div>\n    <div>B</div>\n", "<div>A</div>\n    <div>B</div>")]
        public void TrimsOnlyOuterTemplateWhitespace(string template, string expected) {
            Assert.Equal(expected, Render(template));
        }

        [Fact]
        public void RendersInterpolationsAsEscapedText() {
            var html = Render("<div>{{ state.name }}: {{ state.value + 1 }} / {{ state.missing ?? 'none' }}</div>", new { name = "<Marc>&", value = 2 });

            Assert.Equal("<div>&lt;Marc&gt;&amp;: 3 / none</div>", html);
        }

        [Fact]
        public void DistinguishesTextAndRawHtmlContent() {
            var state = new { html = "<strong>Trusted</strong>" };

            Assert.Equal("<div>&lt;strong&gt;Trusted&lt;/strong&gt;</div>", Render("<div>{{ state.html }}</div>", state));
            Assert.Equal("<div><strong>Trusted</strong></div>", Render("<div x-html=\"state.html\"></div>", state));
            Assert.Equal("<div>&lt;strong&gt;Trusted&lt;/strong&gt;</div>", Render("<div x-text=\"state.html\"></div>", state));
        }

        [Fact]
        public void RendersDynamicAttributesAndEscapesValues() {
            var state = new { url = "?a=1&b=\"'<>", count = 3, enabled = true, hidden = false, missing = (string?)null };
            var html = Render("<a x-attr:href=\"state.url\" x-attr:data-count=\"state.count\" x-attr:enabled=\"state.enabled\" x-attr:hidden=\"state.hidden\" x-attr:missing=\"state.missing\"></a>", state);

            Assert.Equal("<a href=\"?a=1&amp;b=&quot;&#39;&lt;&gt;\" data-count=\"3\" enabled></a>", html);
        }

        [Fact]
        public void UsesTheSameTruthinessForDirectivesAndExpressions() {
            var values = new object?[] { null, false, true, 0, -0d, 2, string.Empty, "value", Array.Empty<object>(), new object() };

            foreach (var value in values) {
                var html = Render("<a x-if=\"state.value\">truthy</a><b x-if=\"!state.value\">falsy</b>", new { value });
                var expected = XTemplateTruthiness(value) ? "<a>truthy</a>" : "<b>falsy</b>";
                Assert.Equal(expected, html);
            }
        }

        [Fact]
        public void UsesTheSameScalarConversionForInterpolationAndStringConcatenation() {
            var values = new object?[] { null, false, true, 12, 1.25d, -0d, "value" };

            foreach (var value in values) {
                var html = Render("<span>{{ state.value }}</span><b>{{ '' + state.value }}</b>", new { value });
                var expected = Scalar(value);
                Assert.Equal($"<span>{expected}</span><b>{expected}</b>", html);
            }
        }

        [Fact]
        public void NormalizesClrNumbersConsistently() {
            foreach (var value in new object[] { 12, 12L, 12.5f, 12.5d, 12.5m }) {
                var normalized = Convert.ToDouble(value).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                var html = Render("<span>{{ state.value + 0 }}</span><b x-attr:data-value=\"state.value\"></b>", new { value });

                Assert.Equal($"<span>{normalized}</span><b data-value=\"{normalized}\"></b>", html);
            }
        }

        [Fact]
        public void RejectsNonFiniteNumbersInBothExpressionAndRendererPaths() {
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("state.value + 0", new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = new { value = double.NaN } }, new[] { new XTemplateReflectionObjectAdapter() })));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => Render("<span>{{ state.value }}</span>", new { value = double.PositiveInfinity }));
            Assert.Throws<XTemplateException>(() => Render("<div x-attr=\"state.attributes\"></div>", new { attributes = new Dictionary<string, object?> { ["value"] = double.NegativeInfinity } }));
        }

        [Fact]
        public void SupportsAttributeSpreadAndDynamicNames() {
            var attributes = new Dictionary<string, object?> { ["title"] = "Hello", ["hidden"] = false, ["checked"] = true, ["count"] = 2, ["empty"] = null };
            var state = new { attributes, attributeName = "data-id", value = "42" };

            var html = Render("<input x-attr=\"state.attributes\" x-attr:[state.attributeName]=\"state.value\">", state);

            Assert.Equal("<input title=\"Hello\" checked count=\"2\" data-id=\"42\">", html);
        }

        [Theory]
        [InlineData("data-id")]
        [InlineData("aria-label")]
        [InlineData("x-custom")]
        [InlineData("xml:lang")]
        [InlineData("xmlns")]
        [InlineData("étiquette")]
        public void RendersValidDynamicAttributeNamesAsOrdinaryAttributes(string name) {
            var html = Render("<div x-attr:[state.name]=\"state.value\"></div>", new { name, value = "value" });

            Assert.Equal($"<div {name}=\"value\"></div>", html);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a b")]
        [InlineData("a=b")]
        [InlineData("a/b")]
        [InlineData("a>b")]
        [InlineData("\u0001")]
        [InlineData("\u007F")]
        [InlineData("\uFDD0")]
        public void RejectsInvalidDynamicAttributeNames(string name) {
            Assert.Throws<XTemplateException>(() => Render("<div x-attr:[state.name]=\"state.value\"></div>", new { name, value = "value" }));
        }

        [Theory]
        [InlineData(0x1FFFE)]
        [InlineData(0x1FFFF)]
        [InlineData(0x10FFFE)]
        [InlineData(0x10FFFF)]
        public void RejectsSupplementaryUnicodeNoncharactersInDynamicAttributeNames(int codePoint) {
            var name = char.ConvertFromUtf32(codePoint);

            Assert.Throws<XTemplateException>(() => Render("<div x-attr:[state.name]=\"state.value\"></div>", new { name, value = "value" }));
        }

        [Fact]
        public void AcceptsOrdinarySupplementaryUnicodeDynamicAttributeNames() {
            var name = char.ConvertFromUtf32(0x1F600);

            var html = Render("<div x-attr:[state.name]=\"state.value\"></div>", new { name, value = "value" });

            Assert.Equal($"<div {name}=\"value\"></div>", html);
        }

        [Theory]
        [InlineData(0x1FFFE)]
        [InlineData(0x1FFFF)]
        [InlineData(0x10FFFE)]
        [InlineData(0x10FFFF)]
        public void RejectsSupplementaryUnicodeNoncharactersInBoundAttributeNames(int codePoint) {
            var name = char.ConvertFromUtf32(codePoint);

            Assert.Throws<XTemplateException>(() => Render($"<div x-attr:{name}=\"state.value\"></div>", new { value = "value" }));
        }

        [Fact]
        public void RendersTruthyShowWithoutHiddenAttribute() {
            var html = Render("<div x-show=\"true\"></div>");

            Assert.Equal("<div></div>", html);
        }

        [Fact]
        public void RendersFalsyShowWithHiddenAttribute() {
            var html = Render("<div x-show=\"false\"></div>");

            Assert.Equal("<div hidden></div>", html);
        }

        [Fact]
        public void PreservesStaticStyleWhenShowIsFalsy() {
            var html = Render("<div class=\"base\" x-class:selected=\"state.selected\" x-class:busy=\"state.busy\" style=\"color:red\" x-show=\"state.visible\"></div>", new { selected = true, busy = false, visible = false });

            Assert.Equal("<div class=\"base selected\" style=\"color:red\" hidden></div>", html);
        }

        [Fact]
        public void ReconcilesExistingHiddenAttributeWithShowWithoutDuplicates() {
            Assert.Equal("<div></div>", Render("<div hidden x-show=\"true\"></div>"));
            Assert.Equal("<div hidden></div>", Render("<div x-show=\"false\" hidden></div>"));
        }

        [Fact]
        public void RendersOnlyTheSelectedConditionalBranch() {
            var template = "<div x-if=\"state.value == 1\">One</div><div x-elseif=\"state.value == 2\">Two</div><div x-else>Other</div>";

            Assert.Equal("<div>Two</div>", Render(template, new { value = 2 }));
            Assert.Equal("<div>Other</div>", Render(template, new { value = 3 }));
        }

        [Fact]
        public void RejectsInvalidConditionalChainsAndConflictingStructuralDirectives() {
            Assert.Throws<XTemplateException>(() => Render("<div x-else>Other</div>"));
            Assert.Throws<XTemplateException>(() => Render("<div x-if=\"true\"></div><span></span><div x-elseif=\"true\"></div>"));
            Assert.Throws<XTemplateException>(() => Render("<div x-if=\"true\" x-for=\"item in state.items\"></div>", new { items = Array.Empty<object>() }));
        }

        [Fact]
        public void RendersLoopsWithScopeAndNestedVariables() {
            var state = new { groups = new[] { new { items = new[] { new { name = "A" }, new { name = "B" } } }, new { items = new[] { new { name = "C" } } } } };
            var html = Render("<div x-for=\"group in state.groups\"><span x-for=\"(item,index) in group.items\">{{ index }}:{{ item.name }}</span></div>", state);

            Assert.Equal("<div><span>0:A</span><span>1:B</span></div><div><span>0:C</span></div>", html);
        }

        [Fact]
        public void NormalizesNumbersStringsAndObjectsForLoops() {
            var dictionary = new Dictionary<string, object?> { ["one"] = 1, ["two"] = 2 };
            var html = Render("<i x-for=\"n in state.number\">{{ n }}</i><b x-for=\"c in state.text\">{{ c }}</b><em x-for=\"key in state.values\">{{ key }}</em>", new { number = 3, text = "A😀", values = dictionary });

            Assert.Equal("<i>1</i><i>2</i><i>3</i><b>A</b><b>😀</b><em>one</em><em>two</em>", html);
        }

        [Fact]
        public void SuppressesBrowserOnlyDirectivesAndKey() {
            var html = Render("<button x-on:click=\"save\" x-prop:value=\"state.value\" x-once x-for=\"item in state.items\" x-key=\"id\">{{ item.id }}</button>", new { value = "x", items = new[] { new { id = 1 }, new { id = 2 } } });

            Assert.Equal("<button>1</button><button>2</button>", html);
        }

        [Theory]
        [InlineData("<div>{{ state.value }}</div>", "<div>value</div>")]
        [InlineData("<div x-text=\"state.value\">child</div>", null)]
        [InlineData("<div x-html=\"state.value\">child</div>", null)]
        [InlineData("<div>{{ state.value }}</span>", null)]
        public void ReportsMalformedTemplates(string template, string? expected) {
            if (expected != null) Assert.Equal(expected, Render(template, new { value = "value" }));
            else Assert.Throws<XTemplateException>(() => Render(template, new { value = "value" }));
        }

        [Fact]
        public void RendersFormatterPipelinesUsingTheConfiguredLocale() {
            var state = new { price = 1234.5 };

            Assert.Equal("<p>1.234,50</p>", Render("<p>{{ state.price | number(2) }}</p>", state, "es-ES"));
            Assert.Equal("<div data-price=\"1.234,50\"></div>", Render("<div x-attr:data-price=\"state.price | number(2)\"></div>", state, "es-ES"));
            Assert.Equal("<p>1,234.50</p>", Render("<p>{{ state.price | number(2) }}</p>", state));
        }

        // methods (private)
        private static string Render(string template, object? state = null, string? locale = null) => new XTemplateRenderer(new[] { new XTemplateReflectionObjectAdapter() }, locale).Render(template, state ?? new { });
        private static bool XTemplateTruthiness(object? value) => value switch { null => false, bool boolean => boolean, double number => number != 0, int number => number != 0, string text => text.Length != 0, _ => true };
        private static string Scalar(object? value) => value switch { null => string.Empty, bool boolean => boolean ? "true" : "false", double number => number == 0 ? "0" : number.ToString("R", System.Globalization.CultureInfo.InvariantCulture), int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture), string text => text, _ => throw new InvalidOperationException() };
    }
}
