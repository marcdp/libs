using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.XVault.Handlers { 
    class EnvHandler : Handler {
        private static readonly Regex PlainPattern = new Regex(@"(?<!\$\{)enc:[^\r\n#""']+", RegexOptions.Compiled);

        public override string Decrypt(string text, string? password, string path) {
            // 1) Load ENV text into an in-memory dictionary-like structure.
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            var order = new List<string>();
            foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n')) {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) {
                    continue;
                }
                var idx = line.IndexOf('=');
                if (idx <= 0) {
                    continue;
                }
                var entryKey = line.Substring(0, idx).Trim();
                var value = line.Substring(idx + 1);
                entries[entryKey] = value;
                order.Add(entryKey);
            }

            // 2) Decode _xvault metadata: xvault:<base64-json>
            if (!entries.TryGetValue("_xvault", out var rawMeta)) {
                throw new Exception("Unable to load vault meta: _xvault field not found.");
            }
            // 3) Derive and validate decryption key.
            var derivedKey = ResolveAndValidateKey(password, rawMeta, path);

            foreach (var keyName in order) {
                if (string.Equals(keyName, "_xvault", StringComparison.Ordinal)) {
                    continue;
                }
                entries[keyName] = DecryptPlaceholders(entries[keyName], derivedKey);
            }

            // Build plain ENV text without _xvault metadata.
            var sb = new StringBuilder();
            foreach (var keyName in order) {
                if (string.Equals(keyName, "_xvault", StringComparison.Ordinal)) {
                    continue;
                }
                sb.Append(keyName).Append('=').Append(entries[keyName]).Append('\n');
            }
            return sb.ToString();
        }

        private string DecryptPlaceholders(string value, byte[] derivedKey) {
            return ReplaceEncryptedTokens(value, derivedKey, PlainPattern);
        }
    }

}