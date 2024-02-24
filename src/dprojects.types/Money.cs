using System;

namespace DProjects.Types {


    public class Money {


        //constructor
        public Money() {
            Amount = 0M;
            Currency = Currency.EUR;
        }
        public Money(decimal amount) {
            Amount = amount;
            Currency = Currency.EUR;
        }
        public Money(decimal amount, Currency currency) {
            Amount = amount;
            Currency = currency;
        }
        public Money(double amount, Currency currency) {
            Amount = (decimal)amount;
            Currency = currency;
        }
        public Money(decimal amount, string currency) {
            Amount = amount;
            Currency = (Currency)(System.Enum.Parse(typeof(Currency), currency, true));
        }
        public Money(double amount, string currency) {
            Amount = (decimal)amount;
            Currency = (Currency)(System.Enum.Parse(typeof(Currency), currency, true));
        }


        //properties
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public int IntegralPart {
            get {
                return Convert.ToInt32(Math.Truncate(Math.Abs(Amount)));
            }
        }
        public int FractionalPart {
            get {
                if (Math.Abs(this.Amount) == this.IntegralPart) {
                    return 0;
                }
                string fractionalPart__1 = System.Convert.ToString((Math.Abs(this.Amount) - this.IntegralPart).ToString().Replace(',', '.'));
                fractionalPart__1 = System.Convert.ToString(fractionalPart__1.Substring(fractionalPart__1.IndexOf('.') + 1).ToString().Substring(0, 2));
                return Convert.ToInt32(fractionalPart__1);
            }
        }


        //methods
        public Money ConvertToCurrency(Money? sourceValue, Currency destinationCurrency, double exchangeRate) {
            if (sourceValue is null || exchangeRate <= 0) throw new InvalidCastException("Wrong amount or exchange rate");
            return new Money(sourceValue.Amount * (decimal)exchangeRate, destinationCurrency);
        }
        public override string ToString() {
            return Amount.ToString() + " " + Currency.ToString();
        }
        public override bool Equals(Object? a) {
            if (a is null) return false;
            if (a.GetType() != typeof(Money)) return false;
            return this == (Money)a;
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }


        //logical operators
        public static bool operator ==(Money firstValue, Money secondValue) {
            if (((object)firstValue) == null || ((object)secondValue) == null) {
                return false;
            }
            if (firstValue.Currency != secondValue.Currency) {
                return false;
            }
            return firstValue.Amount == secondValue.Amount;
        }
        public static bool operator !=(Money firstValue, Money secondValue) {
            return !(firstValue == secondValue);
        }
        public static bool operator >(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidOperationException("Comparison between different currencies is not allowed.");
            }
            return firstValue.Amount > secondValue.Amount;
        }
        public static bool operator <(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidOperationException("Comparison between different currencies is not allowed.");
            }
            if (firstValue == secondValue) {
                return false;
            }
            return !(firstValue > secondValue);
        }
        public static bool operator <=(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidOperationException("Comparison between different currencies is not allowed.");
            }
            if (firstValue < secondValue || firstValue == secondValue) {
                return true;
            }
            return false;
        }
        public static bool operator >=(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidOperationException("Comparison between different currencies is not allowed.");
            }
            if (firstValue > secondValue || firstValue == secondValue) {
                return true;
            }
            return false;
        }


        //aritmetical operators
        public static Money operator +(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidCastException("Calculation is using different currencies!");
            }
            return new Money(firstValue.Amount + secondValue.Amount, firstValue.Currency);
        }
        public static Money operator -(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidCastException("Calculation is using different currencies!");
            }
            return new Money(firstValue.Amount - secondValue.Amount, firstValue.Currency);
        }
        public static Money operator *(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidCastException("Calculation is using different currencies!");
            }
            return new Money(firstValue.Amount * secondValue.Amount, firstValue.Currency);
        }
        public static Money operator /(Money firstValue, Money secondValue) {
            if (firstValue.Currency != secondValue.Currency) {
                throw new InvalidCastException("Calculation is using different currencies!");
            }
            return new Money(firstValue.Amount / secondValue.Amount, firstValue.Currency);
        }


    }


}
