using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;

namespace DProjects.Text.Expressions {


    public class Expression {

        //https://rosettacode.org/wiki/Parsing/Shunting-yard_algorithm#C.23
        //https://blog.kallisti.net.nz/2008/02/extension-to-the-shunting-yard-algorithm-to-allow-variable-numbers-of-arguments-to-functions/

        //constants
        private const string OPERATOR = "OPERATOR";
        private const string NUMBER = "NUMBER";
        private const string BOOLEAN = "BOOLEAN";
        private const string NULL = "NULL";
        private const string VARIABLE = "VARIABLE";
        private const string STRING = "STRING";
        private const string SPACE = "SPACE";
        private const string PARENTHESES = "PARENTHESES";
        private const string CALL = "CALL";
        private const string COMMA = "COMMA";
        private const string ARG = "ARG";

        //operators
        private static readonly Dictionary<string, (string symbol, int precedence, bool rightAssociative)>
            mOperators = new (string symbol, int precedence, bool rightAssociative)[] {
                //math operators
                ("%", 4, true),
                ("*", 3, false),
                ("/", 3, false),
                ("+", 2, false),
                ("-", 2, false),
                //relational operators
                ("==", 1, false),
                ("!=", 1, false),
                (">=", 1, false),
                (">", 1, false),
                ("<=", 1, false),
                ("<", 1, false),
                //logic operators
                ("!", 4, true),
                ("||", 0, true),
                ("&&", 0, true),
        }.ToDictionary(op => op.symbol);

        //global functions
        private static class GlobalMethods {
            public static double Sin(double a) {
                return Math.Sin(a);
            }
            public static double Cos(double a) {
                return Math.Cos(a);
            }
            public static double Sqrt(double a) {
                return Math.Sqrt(a);
            }
            public static double Max(double a, double b) {
                return Math.Max(a, b);
            }
            public static double Min(double a, double b) {
                return Math.Min(a, b);
            }
            public static double Pow(double a, double b) {
                return Math.Pow(a, b);
            }
            public static double Abs(double a) {
                return Math.Abs(a);
            }
            public static double Round(double a) {
                return Math.Round(a);
            }
            public static double Ceiling(double a) {
                return Math.Ceiling(a);
            }
            public static double Floor(double a) {
                return Math.Floor(a);
            }
            public static bool Like(string a, string b) {
                if (a is null || b is null) return false;
                return StringUtils.Like(a, b);
            }
            public static bool Contains(string a, string b) {
                if (a is null) return false;
                return a.IndexOf(b) != -1;
            }
            public static object Call(object instance, string method, params object?[] parameters) {
                var type = instance.GetType();
                var methodInfo = type.GetMethod(method);
                var parameterInfos = methodInfo.GetParameters();
                for (var i = 0; i < parameterInfos.Length; i++) {
                    parameters[i] = ConvertUtils.To(parameters[i], parameterInfos[i].ParameterType, true);
                }
                return methodInfo.Invoke(instance, parameters);
            }
        }

        //variables
        private string mExpression; 
        private object?[] mArguments;
        private MyTokenizer.Token[] mTokens;


        //constructor 
        public Expression(string expression, object?[]? arguments = null) {
            mExpression = expression;
            mArguments = arguments ?? [];
            //tokenize
            var tokens = (new MyTokenizer()).Tokenize(mExpression);
            //infix to postfix
            mTokens = ToPostfix(tokens);
        }


        //properties
        public bool IsEmpty => (mExpression.Length == 0);


        //methods
        public T Eval<T>(IDictionary<string, object?>? variables = null) {
            var result = Eval(variables);
            return ConvertUtils.To<T>(result);
        }
        public object? Eval(IDictionary<string, object?>? variables = null) {
            if (variables == null) variables = new Dictionary<string, object?>();
            var stack = new Stack<object?>();
            void NormalizeOperandType(ref object? a, ref object? b) {
                if (a is bool) {
                    a = ((bool)a ? (decimal)1 : (decimal)0);
                } else if (a is int || a is short || a is byte || a is long || a is double || a is float || a is decimal) {
                    a = Convert.ToDecimal(a);
                } else {
                    a = "" + a;
                }
                if (b is bool) {
                    b = ((bool)b ? (decimal)1 : (decimal)0);
                } else if (b is int || b is short || b is byte || b is long || b is double || b is float || b is decimal) {
                    b = Convert.ToDecimal(b);
                } else {
                    b = "" + b;
                }
                if (a is string || b is string) {
                    a = a.ToString();
                    b = b.ToString();
                }
            }
            foreach (var token in mTokens) {
                if (token.Type == NUMBER) {
                    var value = Decimal.Parse(token.Value, CultureInfo.InvariantCulture);
                    stack.Push(value);
                } else if (token.Type == BOOLEAN) {
                    var value = Boolean.Parse(token.Value);
                    stack.Push(value);
                } else if (token.Type == NULL) {
                    stack.Push(null);
                } else if (token.Type == STRING) {
                    if (token.Value!.StartsWith("'")) {
                        string aux = token.Value!;
                        aux = aux.Substring(1, aux.Length - 2);
                        aux = aux.Replace("\\\'", "'");
                        stack.Push(aux);
                    } else if (token.Value!.StartsWith("\"")) {
                        string aux = token.Value!;
                        aux = aux.Substring(1, aux.Length - 2);
                        aux = aux.Replace("\\\'", "'");
                        aux = aux.Replace("\\\"", "\"");
                        aux = aux.Replace("\\\n", "\n");
                        aux = aux.Replace("\\\r", "\r");
                        aux = aux.Replace("\\\t", "\t");
                        stack.Push(aux);
                    }
                } else if (token.Type == VARIABLE) {
                    if (variables.TryGetValue(token.Value!, out object? value)) {
                        stack.Push(value);
                    } else {
                        throw new ArgumentException("Unable to evaluate expression: variable not found: " + token.Value);
                    }
                } else if (token.Type == ARG) {
                    var value = token.Value!;
                    var indexStr = value.Substring(1, value.Length - 2);
                    if (int.TryParse(indexStr, out int index)) {
                        if (index < 0) throw new ArgumentOutOfRangeException(token.Value);
                        if (index >= mArguments.Length) throw new ArgumentOutOfRangeException(token.Value);
                        stack.Push(mArguments[index]);
                    } else {
                        throw new ArgumentOutOfRangeException(token.Value, "Unable to evaluate expression: arg out: " + token.Value);
                    }
                } else if (token.Type == CALL) {
                    //call
                    var argCount = int.Parse(stack.Pop()!.ToString());
                    var methodName = token.Value!.Trim();
                    var methodInfo = typeof(GlobalMethods).GetMethod(methodName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (methodInfo == null) throw new ArgumentException("Unable to evaluate expression: method not found: " + token.Value);
                    var methodParams = methodInfo.GetParameters();
                    if (methodName.Equals("Call", StringComparison.OrdinalIgnoreCase)) {
                        var callArguments = new List<object?>();
                        for (var i = 0; i < argCount - 2; i++) {
                            callArguments.Add(stack.Pop());
                        }
                        callArguments.Reverse();
                        stack.Push(callArguments.ToArray());
                        argCount = 3;
                    }
                    if (methodParams.Length != argCount) throw new ArgumentException("Unable to evaluate expression: parameter count mismatch: " + token.Value);
                    var methodParamsValues = new List<object?>();
                    var argsConverted = new List<object>();
                    for (var i = 0; i < argCount; i++) {
                        var aux = stack.Pop()!;
                        aux = ConvertUtils.To(aux, methodParams[i].ParameterType, true);
                        methodParamsValues.Add(aux);
                    }
                    methodParamsValues.Reverse();
                    var result = methodInfo.Invoke(null, methodParamsValues.ToArray());
                    stack.Push(result);
                } else if (token.Type == OPERATOR) {
                    switch (token.Value) {
                        //math operators
                        case "%": {
                                var a = ConvertUtils.To<decimal>(stack.Pop());
                                var b = ConvertUtils.To<decimal>(stack.Pop());
                                stack.Push(b % a);
                                break;
                            }
                        case "*": {
                                var a = ConvertUtils.To<decimal>(stack.Pop());
                                var b = ConvertUtils.To<decimal>(stack.Pop());
                                stack.Push(a * b);
                                break;
                            }
                        case "/": {
                                var a = ConvertUtils.To<decimal>(stack.Pop());
                                var b = ConvertUtils.To<decimal>(stack.Pop());
                                stack.Push(b / a);
                                break;
                            }
                        case "+": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                NormalizeOperandType(ref a, ref b);
                                if (a is string && b is string) {
                                    stack.Push((string)b + (string)a);
                                } else {
                                    stack.Push((decimal)a! + (decimal)b!);
                                }
                                break;
                            }
                        case "-": {
                                var a = ConvertUtils.To<decimal>(stack.Pop());
                                var b = ConvertUtils.To<decimal>(stack.Pop());
                                stack.Push(b - a);
                                break;
                            }
                        //relational operators
                        case "==": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null) {
                                    stack.Push(b == null);
                                } else if (b == null) {
                                    stack.Push(a == null);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(a.Equals(b));
                                    } else {
                                        stack.Push((decimal)b! == (decimal)a!);
                                    }
                                }
                                break;
                            }
                        case "!=": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null) {
                                    stack.Push(b != null);
                                } else if (b == null) {
                                    stack.Push(a != null);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(!a.Equals(b));
                                    } else {
                                        stack.Push(!((decimal)b! == (decimal)a!));
                                    }
                                }
                                break;
                            }
                        case "<=": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null || b == null) {
                                    stack.Push(false);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(((string)b).CompareTo((string)a) <= 0);
                                    } else {
                                        stack.Push((decimal)b! <= (decimal)a!);
                                    }
                                }
                                break;
                            }
                        case "<": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null || b == null) {
                                    stack.Push(false);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(((string)b).CompareTo((string)a) < 0);
                                    } else {
                                        stack.Push((decimal)b! < (decimal)a!);
                                    }
                                }
                                break;
                            }
                        case ">=": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null || b == null) {
                                    stack.Push(false);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(((string)b).CompareTo((string)a) >= 0);
                                    } else {
                                        stack.Push((decimal)b! >= (decimal)a!);
                                    }
                                }
                                break;
                            }

                        case ">": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                if (a == null || b == null) {
                                    stack.Push(false);
                                } else {
                                    NormalizeOperandType(ref a, ref b);
                                    if (a is string && b is string) {
                                        stack.Push(((string)b).CompareTo((string)a) > 0);
                                    } else {
                                        stack.Push((decimal)b! > (decimal)a!);
                                    }
                                }
                                break;
                            }
                        //logical operators
                        case "!": {
                                var a = stack.Pop();
                                if (a is bool) {
                                    stack.Push(!(bool)a);
                                } else if (a is string) {
                                    stack.Push(((string)a).Length == 0);
                                } else {
                                    stack.Push(Convert.ToDecimal(a) == 0);
                                }
                                break;
                            }
                        case "&&": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                NormalizeOperandType(ref a, ref b);
                                if (a is string && b is string) {
                                    stack.Push(((string)b).Length > 0 && ((string)a).Length > 0);
                                } else {
                                    var aa = (a is bool ? (bool)a! : ((decimal)a!) != 0);
                                    var bb = (b is bool ? (bool)b! : ((decimal)b!) != 0);
                                    stack.Push(aa && bb);
                                }
                                break;
                            }
                        case "||": {
                                var a = stack.Pop();
                                var b = stack.Pop();
                                NormalizeOperandType(ref a, ref b);
                                if (a is string && b is string) {
                                    stack.Push(((string)b).Length > 0 && ((string)a).Length > 0);
                                } else {
                                    var aa = (a is bool ? (bool)a! : ((decimal)a!) != 0);
                                    var bb = (b is bool ? (bool)b! : ((decimal)b!) != 0);
                                    stack.Push(aa || bb);
                                }
                                break;
                            }
                    }
                }
            }
            return stack.Pop();

        }


        //private methods
        private class MyTokenizer : Tokenizer {
            public MyTokenizer() {
                var tokenDefinitions = new List<TokenDefinition>();
                //spaces or tabs
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(SPACE, (source, index) => {
                    for (var i = index; i < source.Length; i++) {
                        var c = source[i];
                        if (c != ' ' && c != '\t' && c != '\r' && c != '\n') return i - index;
                    }
                    return source.Length - index;
                }));
                //call
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(CALL, (source, index) => {
                    if (!char.IsLetter(source[index])) return 0;
                    for (var i = index; i < source.Length; i++) {
                        var c = source[i];
                        if (i == index && (char.IsLetter(c) || c == '_')) {
                        } else if (i > index && (char.IsLetterOrDigit(c) || c == '_')) {
                        } else if (c == '(') {
                            return i - index;
                        } else {
                            break;
                        }
                    }
                    return 0;
                }));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(COMMA, ",", false));
                //parentheses
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(PARENTHESES, "(", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(PARENTHESES, ")", false));
                //relational operators
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "==", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "!=", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, ">=", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "<=", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, ">", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "<", false));
                //logical
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "!", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "&&", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "||", false));
                //operators
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "%", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "*", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "/", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "+", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(OPERATOR, "-", false));
                //boolean
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(BOOLEAN, "true", false));
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(BOOLEAN, "false", false));
                //null
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(NULL, "null", false));
                //number
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(NUMBER, (source, index) => {
                    if (!char.IsDigit(source[index])) return 0;
                    var dots = 0;
                    for (var i = index; i < source.Length; i++) {
                        var c = source[i];
                        if (char.IsDigit(c)) {
                        } else if (c == '.' && dots++ == 0) {
                        } else {
                            return i - index;
                        }
                    }
                    return source.Length - index;
                }));
                //variable
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(VARIABLE, (source, index) => {
                    if (!char.IsLetter(source[index])) return 0;
                    for (var i = index; i < source.Length; i++) {
                        var c = source[i];
                        if (i == index && (char.IsLetter(c) || c == '_')) {
                        } else if (i > index && (char.IsLetterOrDigit(c) || c == '_')) {
                        } else {
                            return i - index;
                        }
                    }
                    return source.Length - index;
                }));
                //string (double quoted)
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(STRING, (source, index) => {
                    if (source[index] != '"') return 0;
                    var prevCharIsBackslash = false;
                    for (var i = index + 1; i < source.Length; i++) {
                        var c = source[i];
                        if (prevCharIsBackslash) {
                            prevCharIsBackslash = false;
                        } else if (c == '\\') {
                            prevCharIsBackslash = true;
                        } else if (c == '"') {
                            return i - index + 1;
                        }
                    }
                    return source.Length - index;
                }));
                //terminal: single quoted text
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(STRING, (source, index) => {
                    if (source[index] != '\'') return 0;
                    var prevCharIsBackslash = false;
                    for (var i = index + 1; i < source.Length; i++) {
                        var c = source[i];
                        if (prevCharIsBackslash) {
                            prevCharIsBackslash = false;
                        } else if (c == '\\') {
                            prevCharIsBackslash = true;
                        } else if (c == '\'') {
                            return i - index + 1;
                        }
                    }
                    return source.Length - index;
                }));
                //arg: {0}, {1}
                tokenDefinitions.Add(new Tokenizer.TokenDefinition(ARG, (source, index) => {
                    if (source[index] != '{') return 0;
                    for (var i = index + 1; i < source.Length; i++) {
                        var c = source[i];
                        if (c == '}') {
                            return i - index + 1;
                        }
                    }
                    return 0;
                }));
                mTokenDefinitions = tokenDefinitions.ToArray();
            }
        }
        private Tokenizer.Token[] ToPostfix(Tokenizer.Token[] tokens) {
            //infix to postfix
            var stack = new Stack<Tokenizer.Token>();
            var output = new List<Tokenizer.Token>();
            var where = new Stack<bool>();
            var argCount = new Stack<int>();
            foreach (Tokenizer.Token token in tokens) {
                if (token.Type == NUMBER) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == VARIABLE) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == STRING) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == BOOLEAN) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == NULL) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == ARG) {
                    output.Add(token);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                } else if (token.Type == CALL) {
                    stack.Push(token);
                    argCount.Push(0);
                    if (where.Count > 0) {
                        where.Pop();
                        where.Push(true);
                    }
                    where.Push(false);
                } else if (token.Type == COMMA) {
                    Tokenizer.Token? top = null;
                    while (stack.Count > 0 && (top = stack.Pop()).Value != "(") {
                        output.Add(top);
                    }
                    if (top == null || top.Value != "(") {
                        throw new ArgumentException("Unable to parse expression: separator was misplaced or parentheses mismatched");
                    }
                    stack.Push(top);
                    var w = where.Pop();
                    if (w == true) {
                        int a = argCount.Pop();
                        a++;
                        argCount.Push(a);
                    }
                    where.Push(false);
                } else if (token.Type == OPERATOR && mOperators.TryGetValue(token.Value!, out var op1)) {
                    while (stack.Count > 0 && mOperators.TryGetValue(stack.Peek().Value!, out var op2)) {
                        int c = op1.precedence.CompareTo(op2.precedence);
                        if (c < 0 || !op1.rightAssociative && c <= 0) {
                            output.Add(stack.Pop());
                        } else {
                            break;
                        }
                    }
                    stack.Push(token);
                } else if (token.Type == PARENTHESES && token.Value == "(") {
                    stack.Push(token);
                } else if (token.Type == PARENTHESES && token.Value == ")") {
                    Tokenizer.Token? top = null;
                    while (stack.Count > 0 && (top = stack.Pop()).Value != "(") {
                        output.Add(top);
                    }
                    if (stack.Count > 0 && stack.Peek().Type == CALL) {
                        var f = stack.Pop();
                        var a = argCount.Pop();
                        var w = where.Pop();
                        if (w == true) a++;
                        output.Add(new Tokenizer.Token(NUMBER, a.ToString(), new Tokenizer.TokenPosition(0, 0, 0)));
                        output.Add(f);
                    }
                    if (top == null || top.Value != "(") {
                        throw new ArgumentException("Unable to parse expression: no matching left parenthesis.");
                    }
                }
            }
            while (stack.Count > 0) {
                var top = stack.Pop();
                if (!mOperators.ContainsKey(top.Value!)) throw new ArgumentException("Unable to parse expression: no matching right parenthesis.");
                output.Add(top);
            }
            return output.ToArray();
        }
    }



}
