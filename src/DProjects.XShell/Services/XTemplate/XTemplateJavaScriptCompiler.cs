using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class XTemplateJavaScriptCompiler {

        // vars
        private readonly XTemplateCompiler _templateCompiler;

        // ctor
        public XTemplateJavaScriptCompiler(XTemplateCompiler templateCompiler) {
            _templateCompiler = templateCompiler;
        }

        // methods
        public string Transform(string source) {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // locate the exported definition without interpreting strings or comments as source structure
            var document = JavaScriptSource.Parse(source);
            var export = document.FindDefaultExportObject();
            if (export == null) return source;
            var template = export.FindProperty("template");
            if (template == null) throw new InvalidOperationException("The exported X component definition does not declare a static 'template' property.");
            if (!template.Value.IsTemplateLiteral) {
                throw new InvalidOperationException($"The exported X component 'template' at JavaScript offset {template.Value.Start} must be a static template literal.");
            }
            var templateText = DecodeStaticTemplateLiteral(template.Value.Text, template.Value.Start);
            var renderer = SerializeArtifact(_templateCompiler.CompileArtifact(templateText));
            var existingRenderer = export.FindProperty("templateRenderer");
            if (existingRenderer != null) return source[..existingRenderer.Value.Start] + renderer + source[existingRenderer.Value.End..];

            // insert directly after template while retaining all original module text
            var newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var indentation = document.GetLineIndentation(template.KeyStart);
            var insertionOffset = template.SeparatorEnd ?? template.Value.End;
            var prefix = template.SeparatorEnd.HasValue ? "" : ",";
            return source[..insertionOffset] + prefix + newline + indentation + "templateRenderer: " + renderer + "," + source[insertionOffset..];
        }
        public static string InsertDefaultExportProperties(string source, IReadOnlyList<KeyValuePair<string, string>> properties, IReadOnlyList<string> reservedPropertyNames) {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(properties);
            ArgumentNullException.ThrowIfNull(reservedPropertyNames);

            // locate the actual default export without interpreting strings or comments as source structure
            var document = JavaScriptSource.Parse(source);
            var export = document.RequireDefaultExportObject(
                "The HTML X Template module script must contain a default export whose value is an object literal.",
                "The HTML X Template module default export must be an object literal.");
            foreach (var propertyName in reservedPropertyNames) {
                if (export.HasExplicitProperty(propertyName)) {
                    throw new InvalidOperationException($"The HTML X Template module default export already declares '{propertyName}'. The HTML section is authoritative.");
                }
            }

            // insert the generated static properties without reconstructing the surrounding module source
            var newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var indentation = document.GetLineIndentation(export.Start) + "    ";
            var insertion = new StringBuilder();
            foreach (var property in properties) insertion.Append(newline).Append(indentation).Append(property.Key).Append(": ").Append(property.Value).Append(',');
            return source[..export.ContentStart] + insertion + source[export.ContentStart..];
        }

        // methods (private)
        private static string SerializeArtifact(XTemplateCompileResult artifact) {
            var dependencies = artifact.Dependencies.Select(dependency =>
                "{resource:" + JsonSerializer.Serialize(dependency.Resource) + ",ancestorPaths:[" +
                string.Join(',', dependency.AncestorPaths.Select(path => "[" + string.Join(',', path.Select(value => JsonSerializer.Serialize(value))) + "]")) + "]}");
            return "{\n        render:" + artifact.RenderJavaScript + ",\n        dependencies:[" + string.Join(',', dependencies) + "],\n        slots:[" + string.Join(',', artifact.Slots.Select(value => JsonSerializer.Serialize(value))) + "]\n    }";
        }
        private static string DecodeStaticTemplateLiteral(string literal, int sourceOffset) {
            var result = new StringBuilder(literal.Length);
            for (var index = 1; index < literal.Length - 1; index++) {
                var character = literal[index];
                if (character == '$' && index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                    throw new InvalidOperationException($"The X component template at JavaScript offset {sourceOffset} must be static; template substitutions are not supported.");
                }
                if (character != '\\') { result.Append(character); continue; }
                if (++index >= literal.Length - 1) throw new InvalidOperationException($"Invalid escape sequence in template literal at JavaScript offset {sourceOffset + index}.");
                character = literal[index];
                if (character == '\r' || character == '\n') {
                    if (character == '\r' && index + 1 < literal.Length - 1 && literal[index + 1] == '\n') index++;
                    continue;
                }
                result.Append(character switch {
                    'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '0' => '\0',
                    'x' => DecodeFixedEscape(literal, ref index, 2, sourceOffset),
                    'u' => DecodeUnicodeEscape(literal, ref index, sourceOffset),
                    _ => character
                });
            }
            return result.ToString();
        }
        private static string DecodeFixedEscape(string literal, ref int index, int digits, int sourceOffset) {
            if (index + digits >= literal.Length - 1) throw new InvalidOperationException($"Incomplete escape sequence at JavaScript offset {sourceOffset + index}.");
            var hex = literal.Substring(index + 1, digits);
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException($"Invalid escape sequence at JavaScript offset {sourceOffset + index}.");
            index += digits;
            return char.ConvertFromUtf32(value);
        }
        private static string DecodeUnicodeEscape(string literal, ref int index, int sourceOffset) {
            if (index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                var end = literal.IndexOf('}', index + 2);
                if (end < 0 || end >= literal.Length - 1) throw new InvalidOperationException($"Incomplete Unicode escape at JavaScript offset {sourceOffset + index}.");
                var hex = literal[(index + 2)..end];
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value > 0x10FFFF) throw new InvalidOperationException($"Invalid Unicode escape at JavaScript offset {sourceOffset + index}.");
                index = end;
                return char.ConvertFromUtf32(value);
            }
            return DecodeFixedEscape(literal, ref index, 4, sourceOffset);
        }
    }
}
