using System;
using System.Text.RegularExpressions;

namespace DProjects.XVault.Handlers {
    class XmlHandler : Handler {
        private static readonly Regex MetaElementRegex = new Regex(@"<_xvault>\s*([^<\r\n]+)\s*</_xvault>", RegexOptions.Compiled);
        private static readonly Regex MetaAttrRegex = new Regex("\\s_xvault\\s*=\\s*\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex XmlPlainPattern = new Regex(@"(?<!\$\{)enc:[^""'<\r\n]+", RegexOptions.Compiled);

        public override string Decrypt(string text, string? password, string path) {
            string rawMeta;
            string cleaned;

            var elementMatch = MetaElementRegex.Match(text);
            if (elementMatch.Success) {
                rawMeta = elementMatch.Groups[1].Value.Trim();
                cleaned = MetaElementRegex.Replace(text, string.Empty, 1);
            } else {
                var attrMatch = MetaAttrRegex.Match(text);
                if (!attrMatch.Success) {
                    throw new Exception("Unable to load vault meta: _xvault field not found.");
                }
                rawMeta = attrMatch.Groups[1].Value.Trim();
                cleaned = MetaAttrRegex.Replace(text, string.Empty, 1);
            }

            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);
            return ReplaceEncryptedTokens(cleaned, derivedKey, XmlPlainPattern);
        }
    }

}