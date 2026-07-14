using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Handlers {
    class JsonHandler(string text, string path, string? password= null) : Handler {

        // vars
        private static readonly Regex MetaKeyRegex = new Regex("\"_xvault\"\\s*:\\s*\"([^\"]+)\"\\s*,?", RegexOptions.Compiled);
        private static readonly Regex JsonPlainPattern = new Regex(@"(?<!\$\{)enc:[^""\r\n]+", RegexOptions.Compiled);
        private const string MetaComment = "// xvault meta variable (do not modify)\n";

        // methods
        public override string Decrypt() {  
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
        public override void Register(ConfigurationManager configurationManager) { 
            var json = Decrypt();
            configurationManager.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
        }

        // private
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