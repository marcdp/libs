using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace DProjects.XVault {
    abstract class Handler {

        // vars
        protected const string META_PREFIX = "xvault:";
        protected readonly Regex WrappedTokenPattern = new Regex(@"\$\{enc:[^}\r\n]+\}+", RegexOptions.Compiled);

        // methods
        public abstract string Decrypt();
        public abstract void Register(ConfigurationManager configurationManager);

        // protected methods
        protected string DecryptToken(string token, byte[] derivedKey) {
            var blob = DProjects.Utils.Base64Utils.FromBase64UrlSafe(token);
            if (blob.Length < 12 + 16) {
                throw new Exception("Unable to decrypt value: token is truncated.");
            }

            var nonce = new byte[12];
            Buffer.BlockCopy(blob, 0, nonce, 0, 12);

            var ciphertextWithTag = new byte[blob.Length - 12];
            Buffer.BlockCopy(blob, 12, ciphertextWithTag, 0, ciphertextWithTag.Length);

            var cipher = new GcmBlockCipher(new AesEngine());
            var key = new KeyParameter(derivedKey);
            var parameters = new AeadParameters(key, 128, nonce);
            cipher.Init(false, parameters);

            var output = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
            var len = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, output, 0);
            len += cipher.DoFinal(output, len);

            return Encoding.UTF8.GetString(output, 0, len);
        }

        protected byte[] DeriveKey(string? password, string saltHex, string path) {
            if (password == null) {
                var keyring = new Keyrings.KeyringWindows();
                var keyname = "xvault:" + new Uri(path).AbsoluteUri;
                if (keyring.TryReadText(keyname, out string value)) {
                    return DProjects.Utils.Base64Utils.FromBase64(value);
                }
                throw new Exception("Unable to derive key: password not provided and no cached derived key found in keyring.");
            }
            var cfg = new Argon2Config {
                Type = Argon2Type.HybridAddressing, // Argon2id
                Version = Argon2Version.Nineteen,
                Password = Encoding.UTF8.GetBytes(password),
                Salt = DProjects.Utils.HexUtils.HexToBytes(saltHex),
                TimeCost = 5,
                MemoryCost = 131072,
                Lanes = 4,
                Threads = 4,
                HashLength = 32,
            };
            using (var argon2 = new Argon2(cfg)) {
                using (var hash = argon2.Hash()) {
                    var derivedKey = new byte[32];
                    Buffer.BlockCopy(hash.Buffer, 0, derivedKey, 0, 32);
                    return derivedKey;
                }
            }
        }
        protected byte[] ResolveAndValidateKey(string? password, string rawMeta, string path) {

            if (!rawMeta.StartsWith(META_PREFIX, StringComparison.Ordinal)) {
                throw new Exception("Unable to load vault meta: invalid _xvault format.");
            }

            var metaBytes = DProjects.Utils.Base64Utils.FromBase64UrlSafe(rawMeta.Substring(META_PREFIX.Length));
            var metaJson = Encoding.UTF8.GetString(metaBytes);
            using var metaDoc = JsonDocument.Parse(metaJson);
            var meta = metaDoc.RootElement;
            var cryptoVersion = meta.GetProperty("crypto_version").GetInt32();
            var saltHex = meta.GetProperty("salt").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(saltHex)) {
                throw new Exception("Invalid _xvault meta: missing salt.");
            }

            string check = string.Empty;
            if (meta.TryGetProperty("check", out var checkNode) && checkNode.ValueKind != JsonValueKind.Null) {
                check = checkNode.GetString() ?? string.Empty;
            }

            if (cryptoVersion != 1) {
                throw new Exception($"Unable to decrypt: unsupported crypto version {cryptoVersion}.");
            }

            var derivedKey = DeriveKey(password, saltHex, path);
            if (!string.IsNullOrWhiteSpace(check)) {
                try {
                    var checkValue = DecryptToken(check, derivedKey);
                    if (!string.Equals(checkValue, "xvault", StringComparison.Ordinal)) {
                        throw new Exception("Unable to validate password: invalid password.");
                    }
                } catch (InvalidCipherTextException) {
                    throw new Exception("Unable to validate password: invalid password.");
                }
            }

            return derivedKey;
        }

        protected string ReplaceEncryptedTokens(string value, byte[] derivedKey, Regex plainPattern) {
            var result = WrappedTokenPattern.Replace(value, m => {
                var inner = m.Value.Substring(2, m.Value.Length - 3); // enc:...
                var token = inner.Substring(4);
                return DecryptToken(token, derivedKey);
            });
            result = plainPattern.Replace(result, m => {
                var token = m.Value.Substring(4);
                return DecryptToken(token, derivedKey);
            });
            return result;
        }
    }

}