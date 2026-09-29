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
            var templateText = template.Value.GetStaticString();
            if (templateText == null) throw new InvalidOperationException($"The exported X component 'template' at JavaScript offset {template.Value.Start} must be a static string.");

            // pass only the authoritative static component identity into dependency discovery
            var currentComponentName = export.FindProperty("meta")?.Value.AsObject()?.FindProperty("name")?.Value.GetStaticString();
            var renderer = SerializeArtifact(_templateCompiler.CompileArtifact(templateText, currentComponentName));
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
    }
}
