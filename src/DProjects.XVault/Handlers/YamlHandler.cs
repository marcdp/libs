using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Handlers {

    class YamlHandler(string text, string path, string? password = null) : Handler {


        // vars
        private static readonly Regex MetaLineRegex = new Regex(@"_xvault\s*:\s*([^""\r\n]+)", RegexOptions.Compiled);
        private static readonly Regex YamlPlainPattern = new Regex(@"(?<!\$\{)enc:[^""\r\n]+", RegexOptions.Compiled);
        private const string MetaComment = "# xvault meta variable (do not modify)\n";


        // methods
        public override string Decrypt() {
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
        public override void Register(ConfigurationManager configurationManager) {
            throw new NotImplementedException();
        }
    }

}