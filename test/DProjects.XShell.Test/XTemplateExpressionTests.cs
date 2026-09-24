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
        public void AppliesStringLengthAndRejectsNumericStringIndexing() {
            var context = Context(new {
                state = new {
                    text = "A😀",
                    items = new[] { "first", "second" },
                    map = new Dictionary<string, object?> { ["name"] = "Ada" }
                }
            });

            Assert.Equal(3d, XTemplateExpressions.Evaluate("state.text.length", context));
            Assert.Equal("first", XTemplateExpressions.Evaluate("state.items[0]", context));
            Assert.Equal("Ada", XTemplateExpressions.Evaluate("state.map[\"name\"]", context));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("\"abc\"[0]", context));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("\"abc\"[1]", context));
        }

        [Fact]
        public void SupportsStringsEscapesAndConcatenation() {
            var context = Context(new { state = new { name = "Ada" } });

            Assert.Equal("Hello Ada", XTemplateExpressions.Evaluate("'Hello ' + state.name", context));
            Assert.Equal("a\n\t\\\"'", XTemplateExpressions.Evaluate("\"a\\n\\t\\\\\\\"\\'\"", context));
            Assert.Equal("x1true", XTemplateExpressions.Evaluate("'x' + 1 + true", context));
        }

        [Fact]
        public void AcceptsRawSurrogatePairsInStrings() {
            var scalar = char.ConvertFromUtf32(0x10000);

            Assert.Equal(scalar, XTemplateExpressions.Evaluate($"\"{scalar}\"", Context()));
        }

        [Fact]
        public void RejectsUnpairedRawSurrogatesInStrings() {
            var highSurrogate = "\uD800";
            var lowSurrogate = "\uDC00";

            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse($"\"{highSurrogate}\""));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse($"\"{lowSurrogate}\""));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse($"\"{highSurrogate}x\""));
        }

        [Fact]
        public void PreservesEscapedSurrogateValidation() {
            Assert.Equal(char.ConvertFromUtf32(0x10000), XTemplateExpressions.Evaluate(@"""\uD800\uDC00""", Context()));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse(@"""\uD800"""));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse(@"""\uDC00"""));
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
        public void OrdersStringsByUnicodeScalarSequence() {
            var context = Context(new {
                asciiA = "a",
                asciiB = "b",
                prefixShort = "A",
                prefixLong = "AA",
                bmpPrivateUse = "\uE000", // U+E000
                supplementary = "\U00010000", // U+10000
                supplementaryPrefixLower = "A\U00010000", // U+10000 after equal U+0041
                supplementaryPrefixHigher = "A\U00010001" // U+10001 after equal U+0041
            });

            Assert.Equal(true, XTemplateExpressions.Evaluate("asciiA < asciiB", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("prefixShort < prefixLong", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("prefixLong > prefixShort", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("bmpPrivateUse < supplementary", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("supplementary > bmpPrivateUse", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("bmpPrivateUse <= supplementary", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("supplementary >= bmpPrivateUse", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("supplementaryPrefixLower < supplementaryPrefixHigher", context));
            Assert.Equal(true, XTemplateExpressions.Evaluate("supplementaryPrefixHigher > supplementaryPrefixLower", context));
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

        [Fact]
        public void ParsesAndEvaluatesFormatterPipelinesWithConditionalPrecedence() {
            var context = Context(new { state = new { price = 12.5, decimals = 2, name = "  ada  " } });

            var format = Assert.IsType<FormatExpression>(XTemplateExpressions.Parse("state.name | trim | upper"));
            Assert.Equal(new[] { "trim", "upper" }, format.Formatters.Select(formatter => formatter.Name));
            Assert.Equal("13.50", XTemplateExpressions.Evaluate("state.price + 1 | number(2)", context));
            Assert.Equal("A", XTemplateExpressions.Evaluate("true ? 'a' : 'b' | upper", context));
            Assert.Equal("A", XTemplateExpressions.Evaluate("true ? ('a' | upper) : 'b'", context));
            Assert.Equal("B", XTemplateExpressions.Evaluate("false ? 'a' : ('b' | upper)", context));
            Assert.Equal("ADA", XTemplateExpressions.Evaluate("state.name | trim | upper", context));
            Assert.Equal("12.50", XTemplateExpressions.Evaluate("state.price | number(state.decimals)", context));
            Assert.False(XTemplateExpressions.IsAssignable(XTemplateExpressions.Parse("state.price | number(2)")));
        }

        [Fact]
        public void ImplementsFormatterNullShortCircuitingAndErrors() {
            Assert.Null(XTemplateExpressions.Evaluate("null | number(1 / 0) | upper", Context()));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("1 | unknownFormatter", Context()));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("1 | number(-1)", Context()));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse("formatPrice(state.price)"));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse("state.price.toFixed(2)"));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse("1 === 1"));
            Assert.Throws<XTemplateExpressionSyntaxException>(() => XTemplateExpressions.Parse("1 !== 2"));
        }

        [Fact]
        public void FormatsNumbersPercentAndCurrenciesAcrossTheLocaleProfile() {
            Assert.Equal("12", XTemplateExpressions.Evaluate("12 | number", Context()));
            Assert.Equal("12.346", XTemplateExpressions.Evaluate("12.3456 | number", Context()));
            Assert.Equal("12.50", XTemplateExpressions.Evaluate("12.5 | number(2)", Context()));
            Assert.Equal("-1.3", XTemplateExpressions.Evaluate("-1.25 | number(1)", Context()));
            Assert.Equal("1,234.50", XTemplateExpressions.Evaluate("1234.5 | number(2)", Context(locale: "en-US")));
            Assert.Equal("1.234,50", XTemplateExpressions.Evaluate("1234.5 | number(2)", Context(locale: "es-ES")));
            Assert.Equal("25,0\u00A0%", XTemplateExpressions.Evaluate("0.25 | percent(1)", Context(locale: "es-ES")));
            Assert.Equal("€1,234.50", XTemplateExpressions.Evaluate("1234.5 | currency('EUR')", Context(locale: "en-US")));
            Assert.Equal("¥1,235", XTemplateExpressions.Evaluate("1234.5 | currency('JPY')", Context(locale: "en-US")));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("1 | currency('eur')", Context()));
        }

        [Fact]
        public void FormatsDatesTimesAndLocaleAwareTextWithoutLocalTimeConversion() {
            Assert.Equal("24/09/2026", XTemplateExpressions.Evaluate("'2026-09-24' | date('dd/MM/yyyy')", Context()));
            Assert.Equal("septiembre", XTemplateExpressions.Evaluate("'2026-09-24' | date('MMMM')", Context(locale: "es-ES")));
            Assert.Equal("24/09/2026 21:15:00", XTemplateExpressions.Evaluate("'2026-09-24T21:15:00+02:00' | datetime('dd/MM/yyyy HH:mm:ss')", Context()));
            Assert.Equal("21:15", XTemplateExpressions.Evaluate("'2026-09-24T21:15:00+02:00' | time('HH:mm')", Context()));
            Assert.Equal("İ", XTemplateExpressions.Evaluate("'i' | upper", Context(locale: "tr-TR")));
            Assert.Equal("ı", XTemplateExpressions.Evaluate("'I' | lower", Context(locale: "tr-TR")));
            Assert.Equal("value", XTemplateExpressions.Evaluate("'\u2002value\u2002' | trim", Context()));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("'2026-09-24T21:15:00' | datetime('HH:mm')", Context()));
            Assert.Throws<XTemplateExpressionEvaluationException>(() => XTemplateExpressions.Evaluate("'2026-09-24' | date('QQ')", Context()));
        }

        // methods (private)
        private static XTemplateExpressionContext Context(object? values = null, string? locale = null) {
            var identifiers = values == null ? new Dictionary<string, object?>() : values.GetType().GetProperties().ToDictionary(property => property.Name, property => property.GetValue(values));
            return new XTemplateExpressionContext(identifiers, new[] { new XTemplateReflectionObjectAdapter() }, locale);
        }
    }
}
