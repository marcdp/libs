using DProjects.DataTypes;

namespace DProjects.DataTypes.Tests {

    public class CurrencyTests {

        // methods
        [Fact]
        public void EnumIdentity_UsesStableCaseSensitiveCodesAndValueSemantics() {
            Assert.Equal(Currency.USD, Enum.Parse<Currency>("USD"));
            Assert.Throws<ArgumentException>(() => Enum.Parse<Currency>("usd"));
            Assert.True(Enum.TryParse("usd", true, out Currency parsed));
            Assert.Equal(Currency.USD, parsed);
            Assert.Equal("USD", Currency.USD.ToString());
            Assert.Equal(Currency.USD.GetHashCode(), parsed.GetHashCode());
            Assert.NotEqual(Currency.USD, Currency.EUR);
        }
        [Fact]
        public void DescriptionAndSymbol_ReturnKnownMetadataAndCodeFallbacks() {
            Assert.Equal("United States Dollar", Currency.USD.Description());
            Assert.Equal("$", Currency.USD.Symbol());
            Assert.Equal("Canada Dollar", Currency.CAD.Description());
            Assert.Equal("CAD", Currency.CAD.Symbol());
            Assert.Equal("EMF", Currency.EMF.Description());
        }
        [Fact]
        public void InvalidEnumValue_FallsBackToItsNumericRepresentation() {
            var invalid = (Currency)int.MaxValue;

            Assert.Equal(int.MaxValue.ToString(), invalid.Description());
            Assert.Equal(int.MaxValue.ToString(), invalid.Symbol());
        }
    }
}
