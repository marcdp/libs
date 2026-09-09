using Xunit;
using DProjects.DataTypes;

namespace DProjects.DataTypes.Tests
{
    public class MoneyTests {

        [Fact]
        public void Add_2Plus3_Returns5() {
            Money money1 = new Money(2);
            Money money2 = new Money(3);
            Money result = money1 + money2;
            Assert.Equal(5, result.Amount);
        }

        [Fact]
        public void Subtract_5Minus3_Returns2() {
            Money money1 = new Money(5);
            Money money2 = new Money(3);
            Money result = money1 - money2;
            Assert.Equal(2, result.Amount);
        }

        [Fact]
        public void Multiply_5Times3_Returns15() {
            Money money1 = new Money(5);
            Money money2 = new Money(3);
            Money result = money1 * money2;
            Assert.Equal(15, result.Amount);
        }

        [Fact]
        public void Divide_10DividedBy2_Returns5() {
            Money money1 = new Money(10);
            Money money2 = new Money(2);
            Money result = money1 / money2;
            Assert.Equal(5, result.Amount);
        }

        [Fact]
        public void Equals_2And2_ReturnsTrue() {
            Money money1 = new Money(2);
            Money money2 = new Money(2);
            Assert.True(money1.Equals(money2));
            Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
        }

        [Fact]
        public void Equals_2And3_ReturnsFalse() {
            Money money1 = new Money(2);
            Money money2 = new Money(3);
            Assert.False(money1.Equals(money2));
        }
        [Fact]
        public void Equality_HandlesNullReferencesConsistently() {
            Money? first = null;
            Money? second = null;

            Assert.True(first == second);
            Assert.False(first != second);
            Assert.False(new Money(1M) == null);
        }

        [Fact]
        public void ToString_2_Returns2() {
            Money money = new Money(2.0, Currency.EUR);
            Assert.Equal("2 EUR", money.ToString());
        }
        [Fact]
        public void Constructors_PreserveZeroSignsDecimalsCurrenciesAndCaseInsensitiveCodes() {
            Assert.Equal(0M, new Money().Amount);
            Assert.Equal(Currency.EUR, new Money().Currency);
            Assert.Equal(-12.345M, new Money(-12.345M, Currency.USD).Amount);
            Assert.Equal(Currency.USD, new Money(1M, "usd").Currency);
            Assert.Throws<ArgumentException>(() => new Money(1M, "invalid"));
        }
        [Fact]
        public void Comparison_OrdersSameCurrencyAndRejectsDifferentCurrencies() {
            var lower = new Money(-1M, Currency.EUR);
            var equal = new Money(-1M, Currency.EUR);
            var higher = new Money(0M, Currency.EUR);

            Assert.True(lower < higher);
            Assert.True(higher > lower);
            Assert.True(lower <= equal);
            Assert.True(equal >= lower);
            Assert.Throws<InvalidOperationException>(() => lower < new Money(-1M, Currency.USD));
        }
        [Fact]
        public void Arithmetic_PreservesDecimalPrecisionAndRejectsDifferentCurrencies() {
            var first = new Money(0.1M, Currency.EUR);
            var second = new Money(0.2M, Currency.EUR);

            Assert.Equal(0.3M, (first + second).Amount);
            Assert.Equal(-0.1M, (first - second).Amount);
            Assert.Equal(0.02M, (first * second).Amount);
            Assert.Equal(0.5M, (first / second).Amount);
            Assert.Throws<InvalidCastException>(() => first + new Money(0.2M, Currency.USD));
            Assert.Throws<DivideByZeroException>(() => first / new Money(0M, Currency.EUR));
        }
        [Fact]
        public void Conversion_RequiresSourceAndPositiveRateAndDoesNotMutateSource() {
            var source = new Money(10M, Currency.EUR);
            var converted = source.ConvertToCurrency(source, Currency.USD, 1.25);

            Assert.Equal(new Money(12.5M, Currency.USD), converted);
            Assert.Equal(new Money(10M, Currency.EUR), source);
            Assert.Throws<InvalidCastException>(() => source.ConvertToCurrency(null, Currency.USD, 1));
            Assert.Throws<InvalidCastException>(() => source.ConvertToCurrency(source, Currency.USD, 0));
        }
    }
        
}
