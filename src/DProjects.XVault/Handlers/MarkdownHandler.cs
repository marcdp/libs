using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Handlers {
    class MarkdownHandler(string text, string path, string? password = null) : Handler {

        //vars
        private static readonly Regex FrontmatterRegex = new Regex(@"^---\r?\n(.*?)\r?\n---\r?\n?", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex MetaLineRegex = new Regex(@"_xvault\s*:\s*([^""\r\n]+)", RegexOptions.Compiled);
        private static readonly Regex MarkdownPlainPattern = new Regex(@"(?<!\$\{)enc:[^""\r\n]+", RegexOptions.Compiled);


        //methods
        public override string Decrypt() {
            var fmMatch = FrontmatterRegex.Match(text);
            if (!fmMatch.Success) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }

            var frontmatter = fmMatch.Groups[1].Value;
            var body = text.Substring(fmMatch.Length).TrimStart('\n', '\r');

            var metaMatch = MetaLineRegex.Match(frontmatter); 
            if (!metaMatch.Success) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }

            var rawMeta = metaMatch.Groups[1].Value.Trim();
            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);

            return ReplaceEncryptedTokens(body, derivedKey, MarkdownPlainPattern);
        }
        public override void Register(ConfigurationManager configurationManager) {
            throw new NotImplementedException();
        }
    }

}