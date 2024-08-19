using Xunit;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq.Expressions;

namespace DProjects.Text.Expressions.Tests
{
    public class ExpressionTests {

        [Theory()]
        [InlineData("1+2", new object[] { }, 3.0)]
        [InlineData("1+{0}", new object[] { 2 }, 3.0)]
        [InlineData("(1+2)*45", new object[] { }, 135)]
        [InlineData("1+2*45", new object[] { }, 91)]
        [InlineData("1/2", new object[] { }, .5)]
        [InlineData("9 % 1.2", new object[] { }, 0.6)]
        [InlineData("{0} * {1} + {2}", new object[] { 4, 5, 6.5}, 26.5)]
        [InlineData("sin(90)*2", new object[] { }, 1.787993327201116)]
        [InlineData("cos(90)/0.5", new object[] { }, -0.89614723225834)]
        public void MathTest(string expression, object?[] arguments, decimal expected) {
            var exp = new Expression(expression, arguments);
            var result = (decimal) exp.Eval()!;
            Assert.True(Math.Abs(expected - result) < (decimal) 0.000001);
        }


        [Theory()]
        [InlineData("false || false", new object[] { }, false)]
        [InlineData("true || false", new object[] { }, true)]  
        [InlineData("false|| true", new object[] { }, true)]
        [InlineData("true || true", new object[] { }, true)]
        [InlineData("false && false", new object[] { }, false)]
        [InlineData("true && false", new object[] { }, false)]
        [InlineData("false && true", new object[] { }, false)]
        [InlineData("true && true", new object[] { }, true)]
        [InlineData("!true", new object[] { }, false)]
        [InlineData("!false", new object[] { }, true)]
        [InlineData("!!false", new object[] { }, false)]
        public void LogicalTest(string expression, object?[] arguments, bool expected) {
            var exp = new Expression(expression, arguments);
            Assert.Equal(expected , exp.Eval());
        }


        [Theory()]
        [InlineData("1 < 2", new object[] { }, true)]
        [InlineData("2 < 2", new object[] { }, false)]
        [InlineData("3 < 2", new object[] { }, false)]
        [InlineData("1 <= 2", new object[] { }, true)]
        [InlineData("2 <= 2", new object[] { }, true)]
        [InlineData("3 <= 2", new object[] { }, false)]

        [InlineData("1 > 2", new object[] { }, false)]
        [InlineData("2 > 2", new object[] { }, false)]
        [InlineData("3 > 2", new object[] { }, true)]
        [InlineData("1 >= 2", new object[] { }, false)]
        [InlineData("2 >= 2", new object[] { }, true)]
        [InlineData("3 >= 2", new object[] { }, true)]

        [InlineData("1 == 1", new object[] { }, true)]
        [InlineData("1 == 2", new object[] { }, false)]
        [InlineData("2 == 1", new object[] { }, false)]
        [InlineData("1 != 1", new object[] { }, false)]
        [InlineData("1 != 2", new object[] { }, true)]
        [InlineData("2 != 1", new object[] { }, true)]
        
        [InlineData("1 < 2 && 2 <=3 ", new object[] { }, true)]
        [InlineData("1 > 2 || 2 <=3 ", new object[] { }, true)]
        [InlineData("1 > 2 || 2 >3 ", new object[] { }, false)]
        [InlineData("1 > 2 || (2 <3 && false)", new object[] { }, false)]
        [InlineData("1 > 2 || (2 <3 && true)", new object[] { }, true)]
        public void RelationalTest(string expression, object?[] arguments, bool expected) {
            var exp = new Expression(expression, arguments);
            Assert.Equal(expected, exp.Eval());
        }

        [Theory()]
        [InlineData("1 + a", new object[] { }, 2)]
        [InlineData("a +b+c", new object[] { }, 111.5)]
        [InlineData("a +b+c*2", new object[] { }, 212)]
        public void VariablesTest(string expression, object?[] arguments, decimal expected) {
            var exp = new Expression(expression, arguments);
            var variables = new Dictionary<string, object?>();
            variables["a"] = 1;
            variables["b"] = 10;
            variables["c"] = 100.5;
            Assert.Equal(expected, exp.Eval(variables));
        }


        [Theory()]
        [InlineData("1 + 1", new object[] { }, "2.0")]
        [InlineData("1 + 1 + \"hello\"", new object[] { }, "2hello")]
        [InlineData("\"hello\"+\" \"+\"world\"", new object[] { }, "hello world")]
        [InlineData("'hello' + ' ' + 'world'", new object[] { }, "hello world")]
        public void StringsTest(string expression, object?[] arguments, string expected) {
            var exp = new Expression(expression, arguments);
            Assert.Equal(expected, exp.Eval<string>());
        }

        [Theory()]
        [InlineData("call(something, 'MyOperation', 1.5, 2)", new object[] { }, "3.0")]
        [InlineData("call(something, 'MyOperation2', 1.5, 2, 'vars: ')", new object[] { }, "vars: 1.52")]
        public void CallTest(string expression, object?[] arguments, string expected) {
            var exp = new Expression(expression, arguments);
            var variables = new Dictionary<string, object?>();
            variables["something"] = new Something();
            var result = exp.Eval<string>(variables);
            Assert.Equal(expected, result.Replace(",","."));
        }
        private class Something() {
            public double MyOperation(double var1, double var2) {
                return var1 * var2;
            }
            public string MyOperation2(double var1, double var2, string str) {
                return str + var1 + var2;
            }
        }
    }
}
