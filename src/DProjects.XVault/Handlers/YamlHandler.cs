using System;
using System.Text.RegularExpressions;

namespace DProjects.XVault.Handlers {
    class YamlHandler : Handler {
        private static readonly Regex MetaLineRegex = new Regex(@"_xvault\s*:\s*([^""\r\n]+)", RegexOptions.Compiled);
        private static readonly Regex YamlPlainPattern = new Regex(@"(?<!\$\{)enc:[^""\r\n]+", RegexOptions.Compiled);
        private const string MetaComment = "# xvault meta variable (do not modify)\n";

        public override string Decrypt(string text, string? password, string path) {
            var match = MetaLineRegex.Match(text);
            if (!match.Success) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }

            var rawMeta = match.Groups[1].Value.Trim(); 
            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);

            var cleaned = text.Replace(MetaComment, string.Empty);
            cleaned = MetaLineRegex.Replace(cleaned, string.Empty, 1).TrimStart('\n', '\r', ' ');

            return ReplaceEncryptedTokens(cleaned, derivedKey, YamlPlainPattern);
        }
    }

}