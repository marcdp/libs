using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateJavaScriptCompilerTests {

        // methods
        [Fact]
        public void CompilesExpressionsFromValidatedAstThroughSemanticHelpers() {
            var javascript = new XTemplateCompiler().Compile("<p>{{ state.user.name ?? 'Anonymous' }} {{ state.price + 1 | number(2) }}</p>");

            Assert.Contains("utils.expr.member(utils.expr.member(state, \"user\"), \"name\")", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.coalesce", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.add", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.transform", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.scalar", javascript, StringComparison.Ordinal);
            Assert.DoesNotContain("\"\" + (state", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void CompilesLazyControlFlowAndTransformerArguments() {
            var javascript = new XTemplateCompiler().Compile("<div x-if=\"false && (1 / 0)\">{{ null | number(1 / 0) }}</div>");

            Assert.Contains("utils.expr.and(() => false, () => utils.expr.divide(1, 0))", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.transform(null, \"number\", () => [utils.expr.divide(1, 0)], i18n)", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.truthy", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void RejectsHostGlobalsAndUsesLexicalLoopScope() {
            Assert.Throws<XTemplateExpressionSyntaxException>(() => new XTemplateCompiler().Compile("<p>{{ window.location }}</p>"));

            var javascript = new XTemplateCompiler().Compile("<li x-for=\"item in state.items\">{{ item.name }}:{{ index }}</li>");
            Assert.Contains("utils.expr.collection(utils.expr.member(state, \"items\"))", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.member(item, \"name\")", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void CompilesModelWritesAndHiddenAttributeWithoutRawAssignments() {
            var javascript = new XTemplateCompiler().Compile("<input type=\"radio\" value=\"a\" x-model=\"state.choice\"><div x-show=\"state.visible\"></div>");

            Assert.Contains("utils.expr.assign(state, [{kind:\"member\", name:\"choice\"}], value)", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.equal(utils.expr.member(state, \"choice\")", javascript, StringComparison.Ordinal);
            Assert.Contains("hidden:utils.expr.truthy", javascript, StringComparison.Ordinal);
            Assert.DoesNotContain("state.choice = value", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void CompilesLiteralStylesAsStructuredStylesInsteadOfAttributes() {
            var javascript = new XTemplateCompiler().Compile("<div id=\"x\" title=\"y\" style=\" display : none ; width : 100% ; margin-top:8px; --my-value:123; background-image:url('/image?a=b:c;d=e') \" ></div>");

            Assert.Contains("[\"display\"]:{value:\"none\",priority:\"\"}", javascript, StringComparison.Ordinal);
            Assert.Contains("[\"width\"]:{value:\"100%\",priority:\"\"}", javascript, StringComparison.Ordinal);
            Assert.Contains("[\"margin-top\"]:{value:\"8px\",priority:\"\"}", javascript, StringComparison.Ordinal);
            Assert.Contains("[\"--my-value\"]:{value:\"123\",priority:\"\"}", javascript, StringComparison.Ordinal);
            Assert.Contains("[\"background-image\"]:{value:", javascript, StringComparison.Ordinal);
            Assert.Contains("a=b:c;d=e", javascript, StringComparison.Ordinal);
            Assert.Contains("\"id\":utils.rewriteAttribute", javascript, StringComparison.Ordinal);
            Assert.Contains("\"title\":utils.rewriteAttribute", javascript, StringComparison.Ordinal);
            Assert.DoesNotContain("\"style\":utils.rewriteAttribute", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void CompilesImportantAndEmptyLiteralStyles() {
            var important = new XTemplateCompiler().Compile("<div style=\"display:none !important\"></div>");
            var empty = new XTemplateCompiler().Compile("<div style=\"\"></div>");

            Assert.Contains("[\"display\"]:{value:\"none\",priority:\"important\"}", important, StringComparison.Ordinal);
            Assert.Contains("utils.createVDOM(\"div\", null, null, {}, null, {index:0})", empty, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("<div style=\"display\"></div>")]
        [InlineData("<div style=\"display:\"></div>")]
        [InlineData("<div style=\"display:url('x'\"></div>")]
        [InlineData("<div style=\"display:'x\"></div>")]
        public void RejectsMalformedLiteralStyles(string template) {
            var exception = Assert.Throws<XTemplateException>(() => new XTemplateCompiler().Compile(template));

            Assert.Contains("Invalid style declaration", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RejectsStaticallyNamedGenericStyleBindings() {
            var exception = Assert.Throws<XTemplateException>(() => new XTemplateCompiler().Compile("<div x-attr:style=\"state.value\"></div>"));

            Assert.Contains("cannot target 'style'", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RejectsInlineStylesInsideRawPreformattedContent() {
            Assert.Throws<XTemplateException>(() => new XTemplateCompiler().Compile("<div x-pre><span style=\"display:none\"></span></div>"));
        }

        [Theory]
        [InlineData("x-if", "x-elseif")]
        [InlineData("x-if", "x-else")]
        [InlineData("x-if", "x-for")]
        [InlineData("x-if", "x-recursive")]
        [InlineData("x-if", "x-once")]
        [InlineData("x-elseif", "x-else")]
        [InlineData("x-elseif", "x-for")]
        [InlineData("x-elseif", "x-recursive")]
        [InlineData("x-elseif", "x-once")]
        [InlineData("x-else", "x-for")]
        [InlineData("x-else", "x-recursive")]
        [InlineData("x-else", "x-once")]
        [InlineData("x-for", "x-recursive")]
        [InlineData("x-for", "x-once")]
        [InlineData("x-recursive", "x-once")]
        public void RejectsEveryPrimaryStructuralDirectiveCombination(string first, string second) {
            var exception = Assert.Throws<InvalidOperationException>(() => new XTemplateCompiler().Compile($"<div {Attribute(first)} {Attribute(second)}></div>"));

            Assert.Contains("more than one primary structural directive", exception.Message, StringComparison.Ordinal);
            Assert.Contains($"'{first}'", exception.Message, StringComparison.Ordinal);
            Assert.Contains($"'{second}'", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("x-if", "x-for")]
        [InlineData("x-once", "x-recursive")]
        public void RejectsPrimaryStructuralDirectivesIndependentlyOfAttributeOrder(string first, string second) {
            var compiler = new XTemplateCompiler();
            var firstException = Assert.Throws<InvalidOperationException>(() => compiler.Compile($"<div {Attribute(first)} {Attribute(second)}></div>"));
            var secondException = Assert.Throws<InvalidOperationException>(() => compiler.Compile($"<div {Attribute(second)} {Attribute(first)}></div>"));

            Assert.Equal(firstException.Message, secondException.Message);
        }

        [Fact]
        public void AllowsPrimaryStructuralDirectivesWithAuxiliaryAndNonStructuralDirectives() {
            var compiler = new XTemplateCompiler();

            Assert.NotEmpty(compiler.Compile("<div x-if=\"state.visible\" x-show=\"state.enabled\"></div>"));
            Assert.NotEmpty(compiler.Compile("<div x-for=\"item in state.items\" x-key=\"id\" x-class:selected=\"item.selected\"></div>"));
            Assert.NotEmpty(compiler.Compile("<div x-recursive=\"item in state.items\" x-key=\"id\" x-recursive-wrapper=\"ul\"></div>"));
            Assert.NotEmpty(compiler.Compile("<div x-once x-class:ready=\"state.ready\"></div>"));
        }

        [Theory]
        [InlineData("<div x-elseif=\"state.a\"></div>", "x-elseif")]
        [InlineData("<div x-else></div>", "x-else")]
        [InlineData("<div x-if=\"state.a\"></div><span></span><div x-elseif=\"state.b\"></div>", "x-elseif")]
        [InlineData("<div x-if=\"state.a\"></div><span></span><div x-else></div>", "x-else")]
        [InlineData("<div x-if=\"state.a\"></div><div x-else></div><div x-elseif=\"state.b\"></div>", "x-elseif")]
        [InlineData("<div x-if=\"state.a\"></div><div x-else></div><div x-else></div>", "x-else")]
        [InlineData("<div x-if=\"state.a\"></div><div x-for=\"item in state.items\"></div><div x-elseif=\"state.b\"></div>", "x-elseif")]
        [InlineData("<div x-if=\"state.a\"></div><div x-once></div><div x-else></div>", "x-else")]
        [InlineData("<div x-if=\"state.a\"></div>text<div x-else></div>", "x-else")]
        public void RejectsInvalidConditionalChains(string template, string directive) {
            var exception = Assert.Throws<InvalidOperationException>(() => new XTemplateCompiler().Compile(template));

            Assert.Contains($"Directive '{directive}'", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("<div x-if=\"state.a\"></div><div x-elseif=\"state.b\"></div><div x-else></div>")]
        [InlineData("<div x-if=\"state.a\"></div><div x-if=\"state.b\"></div>")]
        [InlineData("<div x-if=\"state.a\"></div><div x-elseif=\"state.b\"></div><div x-if=\"state.c\"></div><div x-else></div>")]
        [InlineData("<div x-if=\"state.a\"></div>\n<!-- formatting -->\n<div x-elseif=\"state.b\"></div>\n<div x-else></div>")]
        [InlineData("<div x-if=\"state.outer\"><span x-if=\"state.innerA\"></span><span x-elseif=\"state.innerB\"></span><span x-else></span></div><div x-else></div>")]
        public void AllowsValidConditionalChains(string template) {
            Assert.NotEmpty(new XTemplateCompiler().Compile(template));
        }

        // methods (private)
        private static string Attribute(string directive) => directive switch {
            "x-if" or "x-elseif" => $"{directive}=\"state.visible\"",
            "x-for" or "x-recursive" => $"{directive}=\"item in state.items\"",
            "x-else" or "x-once" => directive,
            _ => throw new ArgumentOutOfRangeException(nameof(directive))
        };
    }
}
