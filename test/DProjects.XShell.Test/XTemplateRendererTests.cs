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
            Assert.Throws<XTemplateExpressionEvaluationException>(() => Render("<i x-for=\"item in state.items\"></i>", new { items = double.NaN }));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => Render("<i x-for=\"item in state.items\"></i>", new { items = double.PositiveInfinity }));
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
        public void RejectsLiteralInlineStylesInsteadOfSerializingThem() {
            var exception = Assert.Throws<XTemplateException>(() => Render("<div style=\"color:red\"></div>"));

            Assert.Contains("Inline style attributes are not allowed by this XTemplate renderer.", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RejectsInlineStylesFromEveryResolvedGenericAttributePath() {
            var bound = Assert.Throws<XTemplateException>(() => Render("<div x-attr:style=\"state.value\"></div>", new { value = "color:red" }));
            var spread = Assert.Throws<XTemplateException>(() => Render("<div x-attr=\"state.attributes\"></div>", new { attributes = new Dictionary<string, object?> { ["STYLE"] = "color:red" } }));
            var dynamic = Assert.Throws<XTemplateException>(() => Render("<div x-attr:[state.name]=\"state.value\"></div>", new { name = "Style", value = "color:red" }));
            var raw = Assert.Throws<XTemplateException>(() => Render("<div x-pre><span style=\"color:red\"></span></div>"));

            Assert.Contains("Inline style attributes are not allowed by this XTemplate renderer.", bound.Message, StringComparison.Ordinal);
            Assert.Contains("Inline style attributes are not allowed by this XTemplate renderer.", spread.Message, StringComparison.Ordinal);
            Assert.Contains("Inline style attributes are not allowed by this XTemplate renderer.", dynamic.Message, StringComparison.Ordinal);
            Assert.Contains("Inline style attributes are not allowed by this XTemplate renderer.", raw.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RendersInlineStylesFromEverySupportedPathWhenEnabled() {
            Assert.Equal("<div style=\"color:red\"></div>", RenderAllowingStyles("<div style=\"color:red\"></div>"));
            Assert.Equal("<div style=\"color:red\"></div>",
                RenderAllowingStyles("<div x-attr:style=\"state.style\"></div>", new { style = "color:red" }));
            Assert.Equal("<div STYLE=\"color:red\"></div>",
                RenderAllowingStyles("<div x-attr=\"state.attributes\"></div>", new { attributes = new Dictionary<string, object?> { ["STYLE"] = "color:red" } }));
            Assert.Equal("<div Style=\"color:red\"></div>",
                RenderAllowingStyles("<div x-attr:[state.name]=\"state.value\"></div>", new { name = "Style", value = "color:red" }));
            Assert.Equal("<div><span style=\"color:red\"></span></div>", RenderAllowingStyles("<div x-pre><span style=\"color:red\"></span></div>"));
        }

        [Fact]
        public void RendersNamedStylesOnlyWhenStyleSerializationIsEnabled() {
            Assert.Throws<XTemplateException>(() => Render("<div x-style:border=\"'1px solid red'\"></div>"));
            Assert.Equal("<div style=\"border:1px solid red\"></div>", RenderAllowingStyles("<div x-style:border=\"'1px solid red'\"></div>"));
            Assert.Equal("<div style=\"margin-top:8px;--accent-color:red\"></div>", RenderAllowingStyles("<div x-style:margin-top=\"state.margin\" x-style:--accent-color=\"state.accent\"></div>", new { margin = "8px", accent = "red" }));
        }

        [Fact]
        public void NamedStylesSupportScalarNullAndSourceOrderSemantics() {
            Assert.Equal("<div></div>", RenderAllowingStyles("<div x-style:border=\"state.border\"></div>", new { border = (string?)null }));
            Assert.Equal("<div style=\"border:blue;display:block\"></div>", RenderAllowingStyles("<div style=\"border:red;display:block\" x-style:border=\"state.border\"></div>", new { border = "blue" }));
            Assert.Equal("<div style=\"border:red;display:block\"></div>", RenderAllowingStyles("<div x-style:border=\"state.border\" style=\"border:red;display:block\"></div>", new { border = "blue" }));
            Assert.Equal("<div style=\"border:1\"></div>", RenderAllowingStyles("<div x-style:border=\"state.border\"></div>", new { border = 1 }));
            Assert.Equal("<div style=\"border:true\"></div>", RenderAllowingStyles("<div x-style:border=\"state.border\"></div>", new { border = true }));
        }

        [Fact]
        public void NamedStylesRejectObjectsAndRemainOpaqueInsideXPre() {
            Assert.Throws<XTemplateException>(() => RenderAllowingStyles("<div x-style:border=\"state.border\"></div>", new { border = new { value = "red" } }));
            Assert.Equal("<div><span x-style:border=\"'red'\"></span></div>", Render("<div x-pre><span x-style:border=\"'red'\"></span></div>"));
        }

        [Fact]
        public void NamedStylesRejectArraysAndKeepRuntimeImportantAsValueText() {
            Assert.Throws<XTemplateException>(() => RenderAllowingStyles("<div x-style:border=\"state.border\"></div>", new { border = new[] { "red" } }));
            Assert.Equal("<div style=\"display:none !important\"></div>", RenderAllowingStyles("<div x-style:display=\"state.display\"></div>", new { display = "none !important" }));
        }

        [Fact]
        public void WholeObjectStylesExpandIntoTheOrderedStructuredStyleMap() {
            var styles = new Dictionary<string, object?> {
                ["border"] = "1px solid red",
                ["margin-top"] = 8,
                ["--accent-color"] = "blue",
                ["display"] = true,
                ["visibility"] = null
            };

            Assert.Equal("<div style=\"border:1px solid red;margin-top:8;--accent-color:blue;display:true\"></div>", RenderAllowingStyles("<div x-style=\"state.styles\"></div>", new { styles }));
        }

        [Fact]
        public void WholeObjectStylesValidateSourcesMembersNamesAndNullSemantics() {
            Assert.Equal("<div></div>", Render("<div x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["border"] = null } }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = (object?)null }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = new[] { "red" } }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = "red" }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["border"] = new { value = "red" } } }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["border"] = new[] { "red" } } }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["margin.top"] = "red" } }));
        }

        [Fact]
        public void WholeObjectStylesFollowSourceOrderAndStylePolicy() {
            var styles = new Dictionary<string, object?> { ["border"] = "blue" };

            Assert.Equal("<div style=\"border:blue\"></div>", RenderAllowingStyles("<div style=\"border:red\" x-style=\"state.styles\"></div>", new { styles }));
            Assert.Equal("<div style=\"border:red\"></div>", RenderAllowingStyles("<div x-style=\"state.styles\" style=\"border:red\"></div>", new { styles }));
            Assert.Equal("<div style=\"border:blue\"></div>", RenderAllowingStyles("<div x-style=\"state.styles\" x-style:border=\"state.border\"></div>", new { styles, border = "blue" }));
            Assert.Equal("<div style=\"border:red\"></div>", RenderAllowingStyles("<div x-style:border=\"state.border\" x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["border"] = "red" }, border = "blue" }));
            Assert.Throws<XTemplateException>(() => Render("<div x-style=\"state.styles\"></div>", new { styles }));
            Assert.Equal("<div></div>", Render("<div x-style=\"state.styles\"></div>", new { styles = new Dictionary<string, object?> { ["border"] = null } }));
            Assert.Equal("<div><span x-style=\"state.styles\"></span></div>", Render("<div x-pre><span x-style=\"state.styles\"></span></div>", new { styles }));
        }

        [Fact]
        public void EnforcesStylePolicyRecursivelyForRawContent() {
            const string template = "<div x-pre><section><article><span STYLE=\"color:red\"></span></article></section></div>";

            Assert.Throws<XTemplateException>(() => Render(template));
            Assert.Equal("<div><section><article><span STYLE=\"color:red\"></span></article></section></div>", RenderAllowingStyles(template));
        }

        [Fact]
        public void DoesNotTreatRawStyleElementsAsStyleAttributes() {
            const string template = "<div x-pre><style>span { color:red; }</style></div>";

            Assert.Equal("<div><style>span { color:red; }</style></div>", Render(template));
            Assert.Equal("<div><style>span { color:red; }</style></div>", RenderAllowingStyles(template));
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
        public void TreatsNullLoopSourcesAsEmptyCollections() {
            var html = Render("<ul><li x-for=\"item in state.items\">{{ item }}</li></ul>", new { items = (object?)null });

            Assert.Equal("<ul></ul>", html);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData(-1)]
        [InlineData(1.5)]
        public void RejectsInvalidLoopCollectionSources(object items) {
            var exception = Assert.Throws<XTemplateException>(() => Render("<i x-for=\"item in state.items\">{{ item }}</i>", new { items }));

            Assert.Contains("XTemplate collection source", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void SuppressesBrowserOnlyDirectivesAndKey() {
            var html = Render("<button x-on:click=\"save\" x-prop:value=\"state.value\" x-once x-for=\"item in state.items\" x-key=\"id\">{{ item.id }}</button>", new { value = "x", items = new[] { new { id = 1 }, new { id = 2 } } });

            Assert.Equal("<button>1</button><button>2</button>", html);
        }

        [Fact]
        public void AcceptsAndSuppressesWholeObjectPropertyBinding() {
            var html = Render("<x-grid x-prop=\"state.props\"></x-grid>", new { props = new { title = "Grid", count = 2 } });

            Assert.Equal("<x-grid></x-grid>", html);
        }

        [Fact]
        public void PreservesSerializableAttributesWhenSuppressingWholeObjectPropertyBinding() {
            var html = Render("<x-grid id=\"main\" x-prop=\"state.props\"></x-grid>", new { props = new { title = "Grid" } });

            Assert.Equal("<x-grid id=\"main\"></x-grid>", html);
        }

        [Fact]
        public void SuppressesWholeObjectNamedAndDynamicPropertyBindings() {
            var named = Render("<x-grid x-prop=\"state.props\" x-prop:value=\"state.value\"></x-grid>", new { props = new { title = "Grid" }, value = "value" });
            var dynamic = Render("<x-grid x-prop=\"state.props\" x-prop:[state.name]=\"state.value\"></x-grid>", new { props = new { title = "Grid" }, name = "value", value = "value" });

            Assert.Equal("<x-grid></x-grid>", named);
            Assert.Equal("<x-grid></x-grid>", dynamic);
        }

        [Fact]
        public void RejectsMalformedWholeObjectPropertyBindingExpression() {
            Assert.Throws<XTemplateException>(() => Render("<x-grid x-prop=\"state.\"></x-grid>"));
        }

        [Theory]
        [InlineData("<x-grid x-prop></x-grid>")]
        [InlineData("<x-grid x-prop=\"\"></x-grid>")]
        public void RejectsMissingWholeObjectPropertyBindingExpression(string template) {
            Assert.Throws<XTemplateException>(() => Render(template));
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
        public void RendersTransformerPipelinesUsingTheConfiguredLocale() {
            var state = new { price = 1234.5 };

            Assert.Equal("<p>1.234,50</p>", Render("<p>{{ state.price | number(2) }}</p>", state, "es-ES"));
            Assert.Equal("<div data-price=\"1.234,50\"></div>", Render("<div x-attr:data-price=\"state.price | number(2)\"></div>", state, "es-ES"));
            Assert.Equal("<p>1,234.50</p>", Render("<p>{{ state.price | number(2) }}</p>", state));
        }

        [Fact]
        public void RendersPredicateTransformerResultsThroughNormalDirectiveAndScalarSemantics() {
            Assert.Equal("<span>Languages</span>", Render("<span x-if=\"state.type | endsWith('_i18n')\">Languages</span>", new { type = "field_i18n" }));
            Assert.Equal(string.Empty, Render("<span x-if=\"state.type | endsWith('_i18n')\">Languages</span>", new { type = "field" }));
            Assert.Equal("<div></div>", Render("<div x-show=\"state.name | startsWith('A')\"></div>", new { name = "Ada" }));
            Assert.Equal("<div hidden></div>", Render("<div x-show=\"state.name | startsWith('A')\"></div>", new { name = "Bea" }));
            Assert.Equal("<div class=\"match\"></div>", Render("<div x-class:match=\"state.code | contains('-')\"></div>", new { code = "en-US" }));
            Assert.Equal("<p>true</p><p>false</p>", Render("<p>{{ state.match | endsWith('_i18n') }}</p><p>{{ state.noMatch | endsWith('_i18n') }}</p>", new { match = "field_i18n", noMatch = "field" }));
        }

        // methods (private)
        private static string Render(string template, object? state = null, string? locale = null) => new XTemplateRenderer(new[] { new XTemplateReflectionObjectAdapter() }, locale).Render(template, state ?? new { });
        private static string RenderAllowingStyles(string template, object? state = null) {
            return new XTemplateRenderer(new[] { new XTemplateReflectionObjectAdapter() },
                options: new XTemplateRendererOptions { AllowStyleAttributes = true }).Render(template, state ?? new { });
        }
        private static bool XTemplateTruthiness(object? value) => value switch { null => false, bool boolean => boolean, double number => number != 0, int number => number != 0, string text => text.Length != 0, _ => true };
        private static string Scalar(object? value) => value switch { null => string.Empty, bool boolean => boolean ? "true" : "false", double number => number == 0 ? "0" : number.ToString("R", System.Globalization.CultureInfo.InvariantCulture), int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture), string text => text, _ => throw new InvalidOperationException() };
    }
}
