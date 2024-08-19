using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DProjects.Utils.Tests {
    public class StringUtilsTests {


        //compare
        [Theory()]
        [InlineData("hello", "HELLO", true)]
        [InlineData("hello", "HELLOa", false)]
        public void EqualsTest(string value1, string value2, bool result) {
            Assert.Equal(result, StringUtils.Equals(value1,value2));
        }
        [Theory()]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "HELLOa", -1)]
        public void CompareTest(string value1, string value2, int result) {
            Assert.Equal(result, StringUtils.Compare(value1, value2));
        }


        //connection string
        [Theory()]
        [InlineData("VAR1=123;VAR2=234", "VAR1", "123")]
        [InlineData("VAR1=123;VAR2=234", "VAR2", "234")]
        public void GetConnectionStringVariableTest(string connectionString, string variable, string value) {
            Assert.Equal(value, StringUtils.GetConnectionStringVariable(connectionString, variable));
        }
        [Theory()]
        [InlineData("VAR1=123;VAR2=234", "VAR1", "VAR2=234")]
        [InlineData("VAR1=123;VAR2=234", "VAR2", "VAR1=123")]
        [InlineData("VAR0=...;VAR1=123;VAR2=234", "VAR1", "VAR0=...;VAR2=234")]
        public void RemoveConnectionStringVariableTest(string connectionString, string variable, string result) {
            Assert.Equal(result, StringUtils.RemoveConnectionStringVariable(connectionString, variable));
        }
        [Theory()]
        [InlineData("VAR1=123;VAR2=234", new string[] { "VAR1", "VAR2"})]
        public void GetConnectionStringVariableNamesTest(string connectionString, string[] result) {
            Assert.Equal(result, StringUtils.GetConnectionStringVariableNames(connectionString));
        }
        [Theory()]
        [InlineData("VAR1=123;VAR2=234", true)]
        [InlineData("http://host/path?query=123", false)]
        public void SeemsConnectionStringTest(string connectionString, bool result) {
            Assert.Equal(result, StringUtils.SeemsConnectionString(connectionString));
        }


        //size
        [Theory()]
        [InlineData(123, false, false, true, false, "123 bytes", 123)]
        [InlineData(123, true, false, true, false, "1 KB", 1024)]
        [InlineData(123, false, false, true, true, "123bytes", 123)]
        [InlineData(123, true, false, true, true, "1KB", 1024)]
        [InlineData(2*1024, true, false, true, false, "2 KB", 2048)]
        [InlineData(2 * 1024 * 1024, true, false, true, false, "2.0 MB", 2097152)]
        [InlineData(2.45 * 1024 * 1024, true, false, true, false, "2.4 MB", 2516582)]
        [InlineData(1 * 1024 * 1024 * 1024, true, false, true, false, "1.0 GB", 1073741824)]
        [InlineData(1.2 * 1024 * 1024 * 1024, true, false, true, false, "1.2 GB", 1288490189)]
        public void FormatSizeTest(long value, bool minimum1KB, bool returnEmptyFor0Bytes, bool useDotAsDecimalSeparator, bool useNoSpaces, string result, long unformattedBytes) {
            Assert.Equal(result, StringUtils.FormatSize(value, minimum1KB, returnEmptyFor0Bytes,useDotAsDecimalSeparator, useNoSpaces ));
            Assert.Equal(unformattedBytes, StringUtils.UnFormatSize(result));
        }


        //mime
        [Theory()]
        [InlineData("=?iso-8859-1?Q?=A1Hola,_se=F1or!?=", "¡Hola,_señor!")]
        [InlineData("=?utf-8?B?2LPZhNin2YU=?=", "سلام")]
        public void DecodeMimeEncodedStringTest(string text, string result) {
            Assert.Equal(result, StringUtils.DecodeMimeEncodedString(text));
        }


        //split
        [Theory()]
        [InlineData("hello world, how are you", 4, new string[] { "hell", "o wo", "rld,", " how"," are", " you" })]
        public void SplitByColumnsTest(string text, int columns, string[] result) {
            Assert.Equal(result, StringUtils.SplitByColumns(text, columns));
            Assert.Equal(string.Join(Environment.NewLine, result) ,StringUtils.SplitByColumnsAndFold(text, columns));
        }
        [Theory()]
        [InlineData("hello world, how are you", 4, new string[] { "hell", "o wo", "rld,", " how", " are", " you" })]
        public void SplitByColumnsAndFoldTest(string text, int columns, string[] result) {
            Assert.Equal(string.Join(Environment.NewLine, result), StringUtils.SplitByColumnsAndFold(text, columns));
        }


        //cut
        [Theory()]
        [InlineData("hello world, how are you", 10, true, "hello w...")]
        [InlineData("hello world, how are you", 10, false, "hello worl")]
        public void GetTextCuttedTest(string text, int columns, bool addDotsIfRequired, string result) {
            Assert.Equal(result, StringUtils.GetTextCutted(text, columns, addDotsIfRequired));
        }

        //initials
        [Theory()]
        [InlineData("John Doe", "JD")]
        [InlineData("Doe, John", "JD")]
        [InlineData("Miguel Cervantes Saavedra", "MCS")]
        [InlineData("Cervantes Saavedra, Miguel", "MCS")]
        public void ConvertFullNameToInitialsTest(string fullName, string initials) {
            Assert.Equal(initials, StringUtils.ConvertFullNameToInitials(fullName));
        }


        //replace
        [Theory()]
        [InlineData("John Doe?", "johndoe")]
        public void ReplaceASCIICharToAlphaNumericTest(string text, string result) {
            Assert.Equal(result, StringUtils.ReplaceASCIICharToAlphaNumeric(text));
        }
        [Theory()]
        [InlineData("John Dòe?", "john doe?")]
        public void ReplaceASCIICharToASCITest(string text, string result) {
            Assert.Equal(result, StringUtils.ReplaceASCIICharToASCI(text));
        }
        [Theory()]
        [InlineData("John DòÉ?", "John DoE?")]
        public void ReplaceASCIICharToASCICaseSensitiveTest(string text, string result) {
            Assert.Equal(result, StringUtils.ReplaceASCIICharToASCICaseSensitive(text));
        }


        [Theory()]
        [InlineData("John DòÉ?", "dÒé", "kio", "John kio?")]
        public void ReplaceCaseInsensitiveTest(string text, string oldValue, string newValue, string result) {
            Assert.Equal(result, StringUtils.ReplaceCaseInsensitive(text, oldValue, newValue));
        }

        [Theory()]
        [InlineData("John DòÉ?", "dÒé", "kio", StringComparison.OrdinalIgnoreCase,  "John kio?")] 
        public void ReplaceTest(string text, string oldValue, string newValue, StringComparison stringComparison, string result) {
            Assert.Equal(result, StringUtils.Replace(text, oldValue, newValue, stringComparison));
        }
        //[Fact()]
        //public void ReplaceUnicodeSpacingMarkTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}
        //[Fact()]
        //public void ReplaceASCIICharToASCIPrintableTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}


        //indent
        [Theory()]
        [InlineData("Hello", 0)]
        [InlineData(" Hello", 1)]
        [InlineData("  Hello", 2)]
        [InlineData("   Hello", 3)]
        [InlineData("    Hello", 4)]
        public void GetStringIndentTest(string text, int result) {
            Assert.Equal(result, StringUtils.GetStringIndent(text));
        }


        //count
        [Theory()]
        [InlineData("Hello", 'e', 1)]
        [InlineData("Hello", 'l', 2)]
        [InlineData(" Hello world", 'l', 3)]
        public void CountCharactersInStringTest(string text, char c, int result) {
            Assert.Equal(result, StringUtils.CountCharactersInString(text, c));
        }

        //case
        [Theory()]
        [InlineData("hello BIG world", "Hello big world")]
        public void CapitalizeTest(string text, string result) {
            Assert.Equal(result, StringUtils.Capitalize(text));
        }
        [Theory()]
        [InlineData("hello BIG wOrld", "Hello BIG wOrld")]
        public void CapitalizeFirstCharTest(string text, string result) {
            Assert.Equal(result, StringUtils.CapitalizeFirstChar(text));
        } 
        [Theory()]
        [InlineData("hello BIG wOrld", "hello BIG wOrld")]
        public void UnCapitalizeFirstCharTest(string text, string result) {
            Assert.Equal(result, StringUtils.UnCapitalizeFirstChar(text));
        }

        //kebabcase
        [Theory()]
        [InlineData("hello BIG world", "hello-big-world")]
        public void KebabCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.KebabCase(text));
        }

        //snake case
        [Theory()]
        [InlineData("hello BIG world", "hello_big_world")]
        public void SnakeCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.SnakeCase(text));
        }

        //camel case
        [Theory()]
        [InlineData("hello BIG wOrld", "helloBIGWOrld")]
        public void CamelCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.CamelCase(text));
        }
        [Theory()]
        [InlineData("helloBigWorld", "Hello big world")]
        public void CamelToNormalCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.CamelToNormalCase(text));
        }
        
        [Theory()]
        [InlineData("helloBigWorld", "Hello Big World")]
        public void CamelToCapitalizeCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.CamelToCapitalizeCase(text));
        }
        [Theory()]
        [InlineData("helloBigWorld", "hello_big_world")]
        public void CamelToSnakeCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.CamelToSnakeCase(text));
        }
        [Theory()]
        [InlineData("helloBigWorld", "hello-big-world")]
        public void CamelToKebabCaseTest(string text, string result) {
            Assert.Equal(result, StringUtils.CamelToKebabCase(text));
        }

        [Theory()]
        [InlineData("helloBigWorld", false)]
        [InlineData("HELLO", true)]
        [InlineData("HELLO,", true)]
        [InlineData("HELLO,a", false)]
        public void IsAllTextUppercaseTest(string text, bool result) {
            Assert.Equal(result, StringUtils.IsAllTextUppercase(text));
        }


        //padding
        [Theory()]
        [InlineData("hello", 10, "hello     ")]
        public void GetStringRightPaddedWithSpacesTest(string text, int length, string result) {
            Assert.Equal(result, StringUtils.GetStringRightPaddedWithSpaces(text, length));
        }


        //space
        [Theory()]
        [InlineData(' ', 10, "          ")]
        [InlineData('.', 10, "..........")]
        public void SpaceTest(char c, int length, string result) {
            Assert.Equal(result, StringUtils.Space(length, c));
        }


        //like
        [Theory()]
        [InlineData("HELLO", "HE*", true, true)]
        [InlineData("HELLO", "he*", true, true)]
        [InlineData("HELLO", "he*", false, false)]
        [InlineData("HELLO", "*E*", true, true)]
        [InlineData("HELLO", "H?LLO", true, true)]
        [InlineData("HELLO", "H#LLO", true, false)]
        [InlineData("H7LLO", "H#LLO", true, true)]
        [InlineData("HELLO", "H[AEIOU]LLO", true, true)]
        [InlineData("HELLO", "H[AIOU]LLO", true, false)]
        public void LikeTest(string text, string pattern, bool ignoreCase, bool result) {
            Assert.Equal(result, StringUtils.Like( text, pattern, ignoreCase));
        }


        //ascii
        [Theory()]
        [InlineData('A', 65)]
        [InlineData('B', 66)]
        public void AscTest(char c, int result) {
            Assert.Equal(result, StringUtils.Asc(c));
            Assert.Equal(c, StringUtils.Chr(result));
        }
        [Theory()]
        [InlineData('A', 65)]
        [InlineData('B', 66)]
        [InlineData('ç', 231)]
        public void AscWTest(char c, int result) {
            Assert.Equal(result, StringUtils.AscW(c));
            Assert.Equal(c, StringUtils.ChrW(result));
        }



        //is
        [Theory()]
        [InlineData("123", true)]
        [InlineData("123.0", true)]
        [InlineData("", false)]
        [InlineData("1.2", true)]
        [InlineData("1,2", true)]
        public void IsNumericTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsNumeric(expression));
        }
        [Theory()]
        [InlineData("123", true)]
        [InlineData("123.0", false)]
        [InlineData("", false)]
        [InlineData("1.2", false)]
        [InlineData("1,2", false)]
        public void IsIntegerTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsInteger(expression));
        }

        [Theory()]
        [InlineData("123", true)]
        [InlineData("123.0", false)]
        [InlineData("", false)]
        [InlineData("1.2", false)]
        [InlineData("1,2", false)]
        public void IsLongTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsLong(expression));
        }

        //[Theory()]
        //[InlineData("0x12", true)]
        //[InlineData("0xFF", true)]
        //[InlineData("0xFH", false)]
        //[InlineData("0xFFFFFF", true)]
        //[InlineData("0xFFFFFF12", true)]
        //public void IsHexadecimalIntTest(object expression, bool result) {
        //    Assert.Equal(result, StringUtils.IsHexadecimalInt(expression));
        //}

        //[Theory()]
        //[InlineData("0x12", true)]
        //[InlineData("0xFF", true)]
        //[InlineData("0xFH", false)]
        //[InlineData("0xFFFFFF", true)]
        //[InlineData("0xFFFFFF12", true)]
        //public void IsHexadecimalLongTest(object expression, bool result) {
        //    Assert.Equal(result, StringUtils.IsHexadecimalLong(expression));
        //}

        [Theory()]
        [InlineData("2002-01-01", true)]
        public void IsDateTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsDate(expression));
        }

        [Theory()]
        [InlineData("hello@", false)]
        [InlineData("hello@at", false)]
        [InlineData("hello@at.com", true)]
        [InlineData("@at.com", false)]
        [InlineData("@com", false)]
        [InlineData("hello world@host.com", false)]
        public void IsEmailTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsEmail(expression));
        }
        [Theory()]
        [InlineData("123123123", true)]
        [InlineData("123123k", false)]        
        public void IsPhoneTest(string expression, bool result) {
            Assert.Equal(result, StringUtils.IsPhone(expression));
        }
        [Theory()]
        [InlineData("00","626 626 626", "00626626626")]
        [InlineData("+00", "(626)626.626", "00626626626")]
        public void CleanPhoneTest(string prefix, string phone, string result) {
            Assert.Equal(result, StringUtils.CleanPhone(phone, prefix ));
        }

        //infer
        [Theory()]
        [InlineData("123", new Type[] { typeof(int), typeof(long) , typeof(double) })]
        [InlineData("123.3", new Type[] { typeof(double), typeof(DateTime) })]
        [InlineData("false", new Type[] { typeof(bool) })]
        [InlineData("true", new Type[] { typeof(bool) })]
        [InlineData("TRUE", new Type[] { typeof(bool) })]
        [InlineData("0", new Type[] { typeof(int), typeof(long), typeof(double), typeof(bool) })]
        [InlineData("1", new Type[] { typeof(int), typeof(long), typeof(double), typeof(bool) })]
        [InlineData("nan", new Type[] { typeof(int), typeof(long), typeof(double) })]
        [InlineData("inf", new Type[] { typeof(int), typeof(long) })]
        [InlineData("-inf", new Type[] { typeof(int), typeof(long) })]
        [InlineData("2002-01-01T01:00:00", new Type[] { typeof(DateTime) })]
        [InlineData("2002-01-01T01:00:00.000", new Type[] { typeof(DateTime) })]
        [InlineData("2002-01-01T01:00:00.0000", new Type[] { typeof(DateTime) })]
        [InlineData("2002-01-01T01:00:00.00000", new Type[] { typeof(DateTime) })]
        [InlineData("2002-01-01T01:00:00.000000", new Type[] { typeof(DateTime) })]
        [InlineData("2002-01-01T01:00:00.0000000", new Type[] { typeof(DateTime) })]
        public void InferDataTypeTest(string text, Type[] types) {
            Assert.Equal(types, StringUtils.InferDataType(text));
        }


    }
}