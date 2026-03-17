using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DProjects.Text.Expressions {


    public class Tokenizer {


        //Lexer (convert string to tokens)
        //https://blogs.msdn.microsoft.com/drew/2009/12/31/a-simple-lexer-in-c-that-uses-regular-expressions/


        //inner classes
        public delegate int TokenDefinitionDelegate(char[] source, int currentIndex);
        public class TokenDefinition {
            public TokenDefinition(string type, string text) : this(type, null, false, null, text) { }
            public TokenDefinition(string type, string text, bool isIgnored) : this(type, null, isIgnored, null, text) { }
            public TokenDefinition(string type, TokenDefinitionDelegate aDelegate) : this(type, null, false, aDelegate, null) { }
            public TokenDefinition(string type, TokenDefinitionDelegate aDelegate, bool isIgnored) : this(type, null, isIgnored, aDelegate, null) { }
            public TokenDefinition(string type, Regex regex) : this(type, regex, false, null, null) { }
            public TokenDefinition(string type, Regex regex, bool isIgnored) : this(type, regex, isIgnored, null, null) { }
            public TokenDefinition(string type, Regex? regex, bool isIgnored, TokenDefinitionDelegate? aDelegate, string? value) {
                Type = type;
                Regex = regex;
                IsIgnored = isIgnored;
                Delegate = aDelegate;
                Value = value;
            }
            public bool IsIgnored { get; private set; }
            public Regex? Regex { get; private set; }
            public TokenDefinitionDelegate? Delegate { get; private set; }
            public string? Value { get; private set; }
            public string Type { get; private set; }
        }
        public class TokenPosition {
            public TokenPosition(int index, int line, int column) {
                Index = index;
                Line = line;
                Column = column;
            }
            public int Column { get; private set; }
            public int Index { get; private set; }
            public int Line { get; private set; }
        }
        public class Token {
            public Token(string type, string? value, TokenPosition position) {
                Type = type;
                Value = value;
                Position = position;
            }
            public TokenPosition Position { get; set; }
            public string Type { get; set; }
            public string? Value { get; set; }
            public override string ToString() {
                return Type + ":" + Value;
            }
        }


        //variables
        private Regex mEndOfLineRegex = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);
        protected TokenDefinition[] mTokenDefinitions;



        //constructor
        public Tokenizer() {
            mTokenDefinitions = new TokenDefinition[] { };
        }
        public Tokenizer(TokenDefinition[] tokens) {
            mTokenDefinitions = tokens;
        }

        //properties
        public TokenDefinition[] TokenDefinitions => mTokenDefinitions;


        //methods
        public Token[] Tokenize(string source) {
            var result = new List<Token>();
            int currentIndex = 0;
            int currentLine = 1;
            int currentColumn = 1;
            var charArray = source.ToCharArray();
            while (currentIndex < source.Length) {
                TokenDefinition? matchedDefinition = null;
                int matchLength = 0;
                foreach (var rule in mTokenDefinitions) {
                    if (rule.Regex != null) {
                        var match = rule.Regex.Match(source, currentIndex);
                        if (match.Success && (match.Index - currentIndex) == 0) {
                            matchedDefinition = rule;
                            matchLength = match.Length;
                            break;
                        }
                    } else if (rule.Delegate != null) {
                        int aux = rule.Delegate(charArray, currentIndex);
                        if (aux != 0) {
                            matchedDefinition = rule;
                            matchLength = aux;
                            break;
                        }
                    } else if (rule.Value != null) {
                        if (currentIndex + rule.Value.Length <= source.Length) {
                            var aux = source.Substring(currentIndex, rule.Value.Length);
                            if (aux == rule.Value) {
                                matchedDefinition = rule;
                                matchLength = aux.Length;
                                break;
                            }
                        }
                    }
                }
                if (matchedDefinition == null) {
                    throw new Exception(string.Format("Unrecognized symbol '{0}' at index {1} (line {2}, column {3}).", source[currentIndex], currentIndex, currentLine, currentColumn));
                } else {
                    var value = source.Substring(currentIndex, matchLength);
                    if (!matchedDefinition.IsIgnored) {
                        AddToken(result, matchedDefinition.Type, value, currentIndex, currentLine, currentColumn);
                    }
                    var endOfLineMatch = mEndOfLineRegex.Match(value);
                    if (endOfLineMatch.Success) {
                        currentLine += 1;
                        currentColumn = value.Length - (endOfLineMatch.Index + endOfLineMatch.Length) + 1;
                    } else {
                        currentColumn += matchLength;
                    }
                    currentIndex += matchLength;
                }
            }
            AddToken(result, "end", null, currentIndex, currentLine, currentColumn);
            return result.ToArray();
        }


        //utils
        private void AddToken(List<Token> tokens, string type, string? value, int index, int line, int column) {
            tokens.Add(new Token(type, value, new TokenPosition(index, line, column)));
        }

    }

}
