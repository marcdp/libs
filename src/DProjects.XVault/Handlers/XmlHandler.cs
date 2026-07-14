using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Handlers {
    class XmlHandler(string text, string path, string? password = null) : Handler {

        //vars
        private static readonly Regex MetaElementRegex = new Regex(@"<_xvault>\s*([^<\r\n]+)\s*</_xvault>", RegexOptions.Compiled);
        private static readonly Regex MetaAttrRegex = new Regex("\\s_xvault\\s*=\\s*\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex XmlPlainPattern = new Regex(@"(?<!\$\{)enc:[^""'<\r\n]+", RegexOptions.Compiled);


        // methods
        public override string Decrypt() {
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
        public override void Register(ConfigurationManager configurationManager) {
            throw new NotImplementedException();
        }
    }

}