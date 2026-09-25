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
            Assert.Contains("utils.expr.format", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.scalar", javascript, StringComparison.Ordinal);
            Assert.DoesNotContain("\"\" + (state", javascript, StringComparison.Ordinal);
        }

        [Fact]
        public void CompilesLazyControlFlowAndFormatterArguments() {
            var javascript = new XTemplateCompiler().Compile("<div x-if=\"false && (1 / 0)\">{{ null | number(1 / 0) }}</div>");

            Assert.Contains("utils.expr.and(() => false, () => utils.expr.divide(1, 0))", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.format(null, \"number\", () => [utils.expr.divide(1, 0)], i18n)", javascript, StringComparison.Ordinal);
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

            Assert.Contains("utils.expr.setMember(state, \"choice\", value)", javascript, StringComparison.Ordinal);
            Assert.Contains("utils.expr.equal(utils.expr.member(state, \"choice\")", javascript, StringComparison.Ordinal);
            Assert.Contains("hidden:utils.expr.truthy", javascript, StringComparison.Ordinal);
            Assert.DoesNotContain("state.choice = value", javascript, StringComparison.Ordinal);
        }
    }
}
