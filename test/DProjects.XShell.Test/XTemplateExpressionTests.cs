using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Test {

    public sealed class XTemplateExpressionTests {

        // methods
        [Fact]
        public void ParsesStructuralAstWithPrecedence() {
            var expression = XTemplateExpressions.Parse("state.items[index].price + 3 / 12");

            var add = Assert.IsType<BinaryExpression>(expression);
            Assert.Equal("+", add.Operator);
            Assert.IsType<MemberAccessExpression>(add.Left);
            var divide = Assert.IsType<BinaryExpression>(add.Right);
            Assert.Equal("/", divide.Operator);
        }

        [Theory]
        [InlineData("1 + 2 * 3", 7d)]
        [InlineData("(1 + 2) * 3", 9d)]
        [InlineData("10 - 3 - 2", 5d)]
        [InlineData("20 / 5 / 2", 2d)]
        public void EvaluatesArithmeticPrecedenceAndAssociativity(string source, double expected) {
            Assert.Equal(expected, XTemplateExpressions.Evaluate(source, Context()));
        }

        [Fact]
        public void EvaluatesRepresentativeExpressionsAgainstExplicitContext() {
            var context = Context(new {
                state = new { value = 2, array = new[] { new { var2 = 4 } }, index = 0, val1 = false, variable = (string?)null, enabled = true, disabled = false, price = 2, quantity = 3, tax = .5, items = new[] { new { name = "A" }, new { name = "B" } } },
                item = new { name = "A" }, index = 1
            });

            Assert.Equal(3d, XTemplateExpressions.Evaluate("state.value + 1", context));
            Assert.Equal(4.25d, XTemplateExpressions.Evaluate("state.array[state.index].var2 + 3 / 12", context));
            Assert.Equal("232", XTemplateExpressions.Evaluate("state.val1 ? '123' : '232'", context));
            Assert.Equal("default", XTemplateExpressions.Evaluate("state.variable ?? 'default'", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("state.enabled && !state.disabled", context));
            Assert.Equal(6.5d, XTemplateExpressions.Evaluate("(state.price * state.quantity) + state.tax", context));
            Assert.Equal("A", XTemplateExpressions.Evaluate("item.name", context));
            Assert.Equal("B", XTemplateExpressions.Evaluate("state.items[index].name", context));
        }

        [Fact]
        public void UsesCaseSensitiveDataOnlyMemberAccessAndNullPropagation() {
            var context = Context(new { state = new { Name = "Ada", user = (object?)null }, map = new Dictionary<string, object?> { ["value"] = 12 } });

            Assert.Equal("Ada", XTemplateExpressions.Evaluate("state.Name", context));
            Assert.Null(XTemplateExpressions.Evaluate("state.name", context));
            Assert.Equal("Anonymous", XTemplateExpressions.Evaluate("state.user.name ?? 'Anonymous'", context));
            Assert.Equal(12d, XTemplateExpressions.Evaluate("map['value']", context));
            Assert.Null(XTemplateExpressions.Evaluate("state.ToString", context));
        }

        [Fact]
        public void SupportsStringsEscapesAndConcatenation() {
            var context = Context(new { state = new { name = "Ada" } });

            Assert.Equal("Hello Ada", XTemplateExpressions.Evaluate("'Hello ' + state.name", context));
            Assert.Equal("a\n\t\\\"'", XTemplateExpressions.Evaluate("\"a\\n\\t\\\\\\\"\\'\"", context));
            Assert.Equal("x1true", XTemplateExpressions.Evaluate("'x' + 1 + true", context));
        }

        [Fact]
        public void ImplementsStrictEqualityAndComparison() {
            var context = Context();

            Assert.Equal(false, XTemplateExpressions.Evaluate("1 == '1'", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("null == null", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("'a' < 'b'", context));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("true < 1", context));
        }

        [Fact]
        public void ImplementsXTemplateTruthiness() {
            var context = Context(new { empty = "", nonEmpty = "x", zero = 0, number = 2, array = Array.Empty<int>(), data = new { } });

            Assert.Equal(true, XTemplateExpressions.Evaluate("!null && !false && !zero && !empty", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("number && nonEmpty && array && data ? true : false", context));
        }

        [Theory]
        [InlineData("false && (1 / 0)", false)]
        [InlineData("true || (1 / 0)", true)]
        [InlineData("1 ?? (1 / 0)", 1d)]
        [InlineData("true ? 1 : (1 / 0)", 1d)]
        public void ShortCircuitsOperatorsAndConditional(string source, object expected) {
            Assert.Equal(expected, XTemplateExpressions.Evaluate(source, Context()));
        }

        [Fact]
        public void HonorsCoalesceAndConditionalPrecedence() {
            var context = Context(new { a = (object?)null, b = false, c = "yes", d = "no" });

            Assert.Equal("no", XTemplateExpressions.Evaluate("a ?? b ? c : d", context));
            Assert.Equal("fallback", XTemplateExpressions.Evaluate("a ?? a ?? 'fallback'", context));
        }

        [Theory]
        [InlineData("state.name", true)]
        [InlineData("state.items[index].value", true)]
        [InlineData("state", false)]
        [InlineData("state.a + state.b", false)]
        [InlineData("state.value ?? 'default'", false)]
        [InlineData("state.condition ? state.a : state.b", false)]
        public void IdentifiesAssignableExpressions(string source, bool expected) {
            Assert.Equal(expected, XTemplateExpressions.IsAssignable(XTemplateExpressions.Parse(source)));
        }

        [Theory]
        [InlineData("foo()")]
        [InlineData("state.foo()")]
        [InlineData("state.value = 3")]
        [InlineData("state.value++")]
        [InlineData("++state.value")]
        [InlineData("[1, 2, 3]")]
        [InlineData("{ value: 1 }")]
        [InlineData("1e3")]
        [InlineData("'unterminated")]
        public void RejectsUnsupportedSyntax(string source) {
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse(source));
        }

        [Fact]
        public void RejectsUnknownHostIdentifiersAndDoesNotInvokeMethods() {
            var context = Context(new { state = new { value = 1 } });

            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("window.location", context));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Evaluate("state.GetType()", context));
        }

        // methods (private)
        private static XTemplateExpressionContext Context(object? values = null) {
            var identifiers = values == null ? new Dictionary<string, object?>() : values.GetType().GetProperties().ToDictionary(property => property.Name, property => property.GetValue(values));
            return new XTemplateExpressionContext(identifiers);
        }
    }
}
