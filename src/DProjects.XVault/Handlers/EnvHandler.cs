using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Handlers { 
    class EnvHandler(string text, string path, string? password = null) : Handler {

        // vars
        private static readonly Regex MetaLineRegex = new Regex(@"^_xvault\s*=\s*(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex MetaCommentRegex = new Regex(@"^[ \t]*# xvault (?:meta variable \(do not modify\)|metadata\.)[^\r\n]*(?:\r?\n)?",
            RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex PlainPattern = new Regex(@"(?<!\$\{)enc:[^\r\n#""']+", RegexOptions.Compiled);

        // methods
        public override string Decrypt() {
            // locate and validate XVault metadata
            var metaMatch = MetaLineRegex.Match(text);
            if (!metaMatch.Success) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }
            var rawMeta = metaMatch.Groups[1].Value.Trim();
            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);
            // remove only XVault-owned lines and retain surrounding document content
            var cleaned = MetaCommentRegex.Replace(text, string.Empty, 1);
            cleaned = MetaLineRegex.Replace(cleaned, string.Empty, 1).TrimStart('\n', '\r');
            return DecryptPlaceholders(cleaned, derivedKey);
        }
        public override void Register(ConfigurationManager configurationManager) {
            throw new NotSupportedException("Register is not supported for ENV documents.");
        }

        // private
        private string DecryptPlaceholders(string value, byte[] derivedKey) {
            return ReplaceEncryptedTokens(value, derivedKey, PlainPattern);
        }
    }

}
