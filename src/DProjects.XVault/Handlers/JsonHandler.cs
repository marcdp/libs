using System;
using System.Text.RegularExpressions;

namespace DProjects.XVault.Handlers {
    class JsonHandler : Handler {
        private static readonly Regex MetaKeyRegex = new Regex("\"_xvault\"\\s*:\\s*\"([^\"]+)\"\\s*,?", RegexOptions.Compiled);
        private static readonly Regex JsonPlainPattern = new Regex(@"(?<!\$\{)enc:[^""\r\n]+", RegexOptions.Compiled);
        private const string MetaComment = "// xvault meta variable (do not modify)\n";

        public override string Decrypt(string text, string? password, string path) {  
            var match = MetaKeyRegex.Match(text);
            if (!match.Success) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }

            var rawMeta = match.Groups[1].Value;
            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);

            var cleaned = text.Replace(MetaComment, string.Empty);
            cleaned = MetaKeyRegex.Replace(cleaned, string.Empty, 1);

            var indent = DetectJsonIndentation(text);
            cleaned = "{\n" + indent + "\n" + indent + cleaned.TrimStart('{').TrimStart();

            return ReplaceEncryptedTokens(cleaned, derivedKey, JsonPlainPattern);
        }

        private static string DetectJsonIndentation(string text) {
            foreach (var line in text.Split('\n')) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var spaces = line.Length - line.TrimStart(' ').Length;
                if (spaces > 0) {
                    return new string(' ', spaces);
                }
            }
            return "  ";
        }
    }

}