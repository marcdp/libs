using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DProjects.Log {

    // Deliberately supports a small subset: named placeholders, escaped or malformed braces,
    // missing or extra arguments, repetition, and simple alignment/format suffixes.
    internal static class LogMessageTemplate {

        public static Result Parse(string template, object?[] arguments) {
            var rendered = new StringBuilder();
            var nativeTemplate = new StringBuilder();
            var nativeArguments = new List<object?>();
            var fields = new Dictionary<string, object?>();
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            var argumentIndex = 0;
            var index = 0;

            while (index < template.Length) {
                var character = template[index];
                if (character == '{') {
                    if (index + 1 < template.Length && template[index + 1] == '{') {
                        rendered.Append('{');
                        nativeTemplate.Append("{{");
                        index += 2;
                        continue;
                    }

                    var closeIndex = template.IndexOf('}', index + 1);
                    if (closeIndex == -1) {
                        rendered.Append(template.Substring(index));
                        nativeTemplate.Append(template.Substring(index));
                        break;
                    }

                    var expression = template.Substring(index + 1, closeIndex - index - 1);
                    var fieldName = GetFieldName(expression);
                    if (fieldName == null) {
                        AppendEscapedPlaceholder(rendered, nativeTemplate, expression);
                    } else if (argumentIndex < arguments.Length) {
                        var value = arguments[argumentIndex++];
                        rendered.Append(FormatValue(value, expression));
                        nativeTemplate.Append('{').Append(expression).Append('}');
                        nativeArguments.Add(value);
                        fields[fieldName] = value;
                        fieldNames.Add(fieldName);
                    } else {
                        AppendEscapedPlaceholder(rendered, nativeTemplate, expression);
                    }
                    index = closeIndex + 1;
                    continue;
                }

                if (character == '}') {
                    rendered.Append('}');
                    if (index + 1 < template.Length && template[index + 1] == '}') {
                        nativeTemplate.Append("}}");
                        index += 2;
                    } else {
                        nativeTemplate.Append('}');
                        index++;
                    }
                    continue;
                }

                rendered.Append(character);
                nativeTemplate.Append(character);
                index++;
            }

            return new Result(
                rendered.ToString(),
                nativeArguments.Count == 0 ? rendered.ToString() : nativeTemplate.ToString(),
                nativeArguments.ToArray(),
                fields,
                fieldNames
            );
        }

        public static string EscapeLiteral(string? value) {
            return (value ?? "").Replace("{", "{{").Replace("}", "}}");
        }

        private static void AppendEscapedPlaceholder(
            StringBuilder rendered,
            StringBuilder nativeTemplate,
            string expression
        ) {
            rendered.Append('{').Append(expression).Append('}');
            nativeTemplate.Append("{{").Append(expression).Append("}}");
        }

        private static string? GetFieldName(string expression) {
            var delimiterIndex = GetDelimiterIndex(expression);
            var fieldName = (delimiterIndex == -1 ? expression : expression.Substring(0, delimiterIndex)).Trim();
            if (fieldName.StartsWith("@", StringComparison.Ordinal)
                || fieldName.StartsWith("$", StringComparison.Ordinal)) {
                fieldName = fieldName.Substring(1);
            }
            return fieldName.Length == 0 ? null : fieldName;
        }

        private static string FormatValue(object? value, string expression) {
            var delimiterIndex = GetDelimiterIndex(expression);
            if (delimiterIndex == -1) {
                return value?.ToString() ?? "";
            }

            try {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "{0" + expression.Substring(delimiterIndex) + "}",
                    value
                );
            } catch (FormatException) {
                return value?.ToString() ?? "";
            }
        }

        private static int GetDelimiterIndex(string expression) {
            var alignmentIndex = expression.IndexOf(',');
            var formatIndex = expression.IndexOf(':');
            if (alignmentIndex == -1) {
                return formatIndex;
            }
            if (formatIndex == -1) {
                return alignmentIndex;
            }
            return Math.Min(alignmentIndex, formatIndex);
        }

        internal sealed class Result {
            public Result(
                string renderedMessage,
                string nativeTemplate,
                object?[] nativeArguments,
                Dictionary<string, object?> fields,
                HashSet<string> fieldNames
            ) {
                RenderedMessage = renderedMessage;
                NativeTemplate = nativeTemplate;
                NativeArguments = nativeArguments;
                Fields = fields;
                FieldNames = fieldNames;
            }

            public string RenderedMessage { get; }
            public string NativeTemplate { get; }
            public object?[] NativeArguments { get; }
            public Dictionary<string, object?> Fields { get; }
            public HashSet<string> FieldNames { get; }
        }
    }
}
