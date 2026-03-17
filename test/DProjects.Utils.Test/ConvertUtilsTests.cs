using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Drawing;
using System.Runtime.InteropServices.ObjectiveC;
using System.Net;
using System.Globalization;
using System.Xml;

namespace DProjects.Utils.Tests {


    public class ConvertUtilsTests {

        [Theory]
        [InlineData(null, "")]
        [InlineData(true, "Y")]
        [InlineData(false, "N")]
        [InlineData((float)1.234, "1.234")]
        [InlineData((double)1.234, "1.234")]
        [InlineData((int)-1, "")]
        [InlineData((short)123, "123")]
        [InlineData((int)123, "123")]
        [InlineData((long)123, "123")]
        [InlineData("date:2020-01-02T16:23:34", "2020-01-02T16:23:34")]
        [InlineData(new string[] { "hello","world"}, "hello,world")]
        [InlineData(new object?[] { 123, "world", "false", null }, "123,world,false,null")]
        [InlineData(new char[] { 'a','b','c' }, "a,b,c")]
        [InlineData("dict:", "var1=123&var2=False")]
        [InlineData("hello world", "hello...", 8)]
        [InlineData("color:#FF0012" , "#FF0012")]
        public void ToSimpleStringTest(object value, string result, int maxLength= 123) {
            value = ProcessEncodedValue(value);
            if (value != null && value.Equals("dict:")) {
                var dict = new Dictionary<string, object>();
                dict.Add("var1", 123);
                dict.Add("var2", false);
                value = dict;
            }
            Assert.Equal(result, ConvertUtils.ToSimpleString(value, new() {  MaxLength = maxLength }));
        }

        [Theory]
        [InlineData("short", typeof(short))]
        [InlineData("int", typeof(int))]
        [InlineData("long", typeof(long))]
        [InlineData("bool", typeof(bool))]
        [InlineData("float", typeof(float))]
        [InlineData("double", typeof(double))]
        [InlineData("decimal", typeof(decimal))]
        [InlineData("string", typeof(string))]
        [InlineData("date", typeof(DateTime))]
        [InlineData("datetime", typeof(DateTime))]
        [InlineData("time", typeof(TimeSpan))]
        [InlineData("guid", typeof(Guid))]
        [InlineData("byte", typeof(byte))]
        [InlineData("char", typeof(char))]
        [InlineData("short[]", typeof(short[]))]
        [InlineData("int[]", typeof(int[]))]
        [InlineData("long[]", typeof(long[]))]
        [InlineData("bool[]", typeof(bool[]))]
        [InlineData("float[]", typeof(float[]))]
        [InlineData("double[]", typeof(double[]))]
        [InlineData("decimal[]", typeof(decimal[]))]
        [InlineData("string[]", typeof(string[]))]
        [InlineData("date[]", typeof(DateTime[]))]
        [InlineData("time[]", typeof(TimeSpan[]))]
        [InlineData("guid[]", typeof(Guid[]))]
        [InlineData("byte[]", typeof(byte[]))]
        [InlineData("char[]", typeof(char[]))]
        public void ToSimpleTypeTest(string name, Type result) {
            Assert.Equal(result, ConvertUtils.ToSimpleType(name));
            if (name == "date") name = "datetime";
            if (name == "date[]") name = "datetime[]";
            Assert.Equal(name, ConvertUtils.FromSimpleType(result));            
        }

        [Theory]
        [InlineData(new byte[] { 1, 2 ,3,4,5,6,78,56,34,0}, "0102030405064E382200")]
        public void ToHexString(byte[] buffer, string result) {
            Assert.Equal(result, ConvertUtils.ToHexString(buffer));
            Assert.Equal(buffer, ConvertUtils.FromHexString(result));
        }

        [Theory]
        [InlineData(1231231, "1970-01-15T06:00:31.0000000Z")]
        [InlineData(91231145245, "4860-12-31T00:47:25.0000000Z")]
        public void FromEpochSeconds(long epoch, string result) {
            Assert.Equal(result, ConvertUtils.FromEpochSeconds(epoch).ToString(DateTimeUtils.DATETIME_ISO8601_MS7));
        }

        [Theory]
        [InlineData(1231231, "1970-01-01T00:20:31.2310000Z")]
        [InlineData(91231145245, "1972-11-21T21:59:05.2450000Z")]
        public void FromEpochMilliSeconds(long epoch, string result) {
            Assert.Equal(result, ConvertUtils.FromEpochMilliSeconds(epoch).ToString(DateTimeUtils.DATETIME_ISO8601_MS7));
        }

        [Theory]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("N", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("Y", true)]
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("True", true)]
        public void ToBoolean(string text, bool result) {
            Assert.Equal(result, ConvertUtils.ToBoolean(text));
        }

        [Theory]
        [InlineData("red", "#FF0000")]
        [InlineData("#F15", "#F01050")]
        [InlineData("rgb(255,1,5)", "#FF0105")]
        public void ToColor(string text, string result) {
            Assert.Equal(result, ConvertUtils.ToSimpleString(ConvertUtils.ToColor(text)));
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("0", 0)]
        [InlineData("-1", -1)]
        public void ToInteger(string text, int result) {
            Assert.Equal(result, ConvertUtils.ToInteger(text));
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("0", 0)]
        [InlineData("-1", -1)]
        public void ToLong(string text, long result) {
            Assert.Equal(result, ConvertUtils.ToLong(text));
        }


        [Theory]
        [InlineData("123.8", 123.8)]
        [InlineData("0.9", 0.9)]
        [InlineData("0", 0)]
        [InlineData("-1", -1)]
        public void ToDouble(string text, double result) {
            Assert.Equal(result, ConvertUtils.ToDouble(text));
        }

        [Theory]
        [InlineData("123.8213", 123.8213)]
        [InlineData("0.923", 0.923)]
        public void ToDecimal(string text, decimal result) {
            Assert.Equal(result, ConvertUtils.ToDecimal(text));
        }

        [Theory]
        [InlineData("123.8213", 123.8213)]
        [InlineData("0.923", 0.923)]
        public void ToSingle(string text, float result) {
            Assert.Equal(result, ConvertUtils.ToSingle(text));
        }


        [Theory]
        [InlineData("a,b,c", ',', new string[] { "a","b","c"} )]
        [InlineData("a,123,false", ',', new string[] { "a", "123", "false" })]
        public void ToStringA(string text, char separator, string[] result) {
            Assert.Equal(result, ConvertUtils.ToStringA(text, separator));
        }

        [Theory]
        [InlineData("123", typeof(int), 123)]
        [InlineData("date:2020-01-02T16:23:34", typeof(DateTime), "date:2020-01-02T16:23:34")]
        [InlineData("color:#f15", typeof(Color), "color:#F01050")]
        [InlineData("1", typeof(bool), true)]
        [InlineData("TRUE", typeof(bool), true)]
        [InlineData("Y", typeof(bool), true)]
        [InlineData("Yes", typeof(bool), true)]
        [InlineData("0", typeof(bool), false)]
        [InlineData("FALSE", typeof(bool), false)]
        [InlineData("N", typeof(bool), false)]
        [InlineData("False", typeof(bool), false)]
        [InlineData("1.123", typeof(double), 1.123)]
        [InlineData("1,456", typeof(double), (double)1456)]
        [InlineData("1.123", typeof(float), (float)1.123)]
        [InlineData("1,456", typeof(float), (float)1456)]
        [InlineData("aGVsbG8=", typeof(byte[]), new byte[] {104, 101, 108, 108, 111})]
        [InlineData("a,b,c", typeof(string[]), new string[] { "a", "b", "c"})]
        [InlineData("Ordinal", typeof(System.StringComparison), System.StringComparison.Ordinal)]
        [InlineData("", typeof(System.Net.IPAddress), "ip:any")]
        [InlineData("*", typeof(System.Net.IPAddress), "ip:any")]
        [InlineData("192.168.0.1", typeof(System.Net.IPAddress), "ip:192.168.0.1")]
        [InlineData("es", typeof(CultureInfo), "culture:es")]
        [InlineData("utf-8", typeof(Encoding), "encoding:utf-8")]
        [InlineData("UTF-8", typeof(Encoding), "encoding:utf-8")]
        [InlineData("UTF-16", typeof(Encoding), "encoding:utf-16")]
        [InlineData("UTF-32", typeof(Encoding), "encoding:utf-32")]
        [InlineData("int", typeof(Type), typeof(int))]
        [InlineData("string", typeof(Type), typeof(string))]
        [InlineData("DProjects.Utils.ConvertUtils", typeof(Type), typeof(ConvertUtils))]
        [InlineData("54b91632-1cfc-4d1f-9720-68e5081be81d", typeof(Guid), "guid:54b91632-1cfc-4d1f-9720-68e5081be81d")]
        [InlineData("1.2.3.4", typeof(Version), "version:1.2.3.4")]
        [InlineData("<node>123</node>", typeof(XmlDocument), "xml:<node>123</node>")]
        public void To(object value, Type type, object result) {
            value = ProcessEncodedValue(value);
            result = ProcessEncodedValue(result);
            Assert.Equal(result, ConvertUtils.To(value, type, false));
        }


        //private utils        
        private object ProcessEncodedValue(object? value) {
            if (value != null && value.ToString()!.StartsWith("date:")) value = DateTime.Parse(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("color:")) value = ConvertUtils.ToColor(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("ip:any")) value = IPAddress.Any;
            if (value != null && value.ToString()!.StartsWith("ip:loopback")) value = IPAddress.Loopback;
            if (value != null && value.ToString()!.StartsWith("ip:")) value = IPAddress.Parse(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("culture:")) value = new CultureInfo(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("encoding:")) {
                EncodingUtils.RegisterDefaultProvider();
                value = Encoding.GetEncoding(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            }
            if (value != null && value.ToString()!.StartsWith("guid:")) value = new Guid(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("version:")) value = new Version(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("xml:")) value = XmlUtils.LoadXml(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            return value!;
        }
    }
}