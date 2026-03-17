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
        }

        [Fact]
        public void Equals_2And3_ReturnsFalse() {
            Money money1 = new Money(2);
            Money money2 = new Money(3);
            Assert.False(money1.Equals(money2));
        }

        [Fact]
        public void ToString_2_Returns2() {
            Money money = new Money(2.0, Currency.EUR);
            Assert.Equal("2 EUR", money.ToString());
        }
    }
        
}