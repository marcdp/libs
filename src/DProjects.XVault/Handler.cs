using System;
using System.Collections.Generic;
using System.IO;
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

        // consts
        protected const string META_PREFIX = "xvault:";
        private const int AesKeyLength = 32;
        private const int GcmNonceLength = 12;
        private const int GcmTagLength = 16;

        // vars
        private static readonly byte[] V2DekAssociatedData = Encoding.UTF8.GetBytes("xvault:v2:dek");
        protected readonly Regex WrappedTokenPattern = new Regex(@"\$\{enc:[^}\r\n]+\}+", RegexOptions.Compiled);

        // methods
        public abstract string Decrypt();
        public abstract void Register(ConfigurationManager configurationManager);

        // protected methods
        protected string DecryptToken(string token, byte[] key) {
            var blob = DProjects.Utils.Base64Utils.FromBase64UrlSafe(token);
            return Encoding.UTF8.GetString(DecryptGcm(blob, key));
        }
        protected byte[] DeriveKey(string? password, string saltHex, string path, int cryptoVersion) {
            if (password == null) {
                var keyring = new Keyrings.KeyringWindows();
                var keyname = GetKeyringName(path, cryptoVersion);
                if (keyring.TryReadText(keyname, out string value)) {
                    try {
                        var cachedKey = DProjects.Utils.Base64Utils.FromBase64(value);
                        if (cachedKey.Length != AesKeyLength) {
                            throw new Exception("Unable to validate password: invalid cached key.");
                        }
                        return cachedKey;
                    } catch (FormatException error) {
                        throw new Exception("Unable to validate password: invalid cached key.", error);
                    }
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
            // decode and validate the common metadata envelope
            if (!rawMeta.StartsWith(META_PREFIX, StringComparison.Ordinal)) {
                throw new Exception("Unable to load vault meta: invalid _xvault format.");
            }

            var metaBytes = DProjects.Utils.Base64Utils.FromBase64UrlSafe(rawMeta.Substring(META_PREFIX.Length));
            var metaJson = Encoding.UTF8.GetString(metaBytes);
            using var metaDoc = JsonDocument.Parse(metaJson);
            var meta = metaDoc.RootElement;
            if (meta.ValueKind != JsonValueKind.Object) {
                throw new Exception("Unable to load vault meta: expected an object.");
            }
            var cryptoVersion = meta.GetProperty("crypto_version").GetInt32();
            if (cryptoVersion != 1 && cryptoVersion != 2) {
                throw new Exception($"Unable to decrypt: unsupported crypto version {cryptoVersion}.");
            }

            var saltHex = meta.TryGetProperty("salt", out var saltNode) && saltNode.ValueKind == JsonValueKind.String
                ? saltNode.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(saltHex)) {
                throw new Exception("Invalid _xvault meta: missing salt.");
            }

            if (cryptoVersion == 2) {
                return ResolveV2Key(password, meta, saltHex, path);
            }

            // preserve the historical v1 optional check behavior
            string check = string.Empty;
            if (meta.TryGetProperty("check", out var checkNode) && checkNode.ValueKind != JsonValueKind.Null) {
                check = checkNode.GetString() ?? string.Empty;
            }

            var derivedKey = DeriveKey(password, saltHex, path, cryptoVersion);
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
        internal static string GetKeyringName(string path, int cryptoVersion) {
            // mirror canonical Python's version-specific service name and absolute file URI
            var prefix = cryptoVersion == 1 ? "xvault:" : $"xvault:v{cryptoVersion}:";
            return prefix + new Uri(Path.GetFullPath(path)).AbsoluteUri;
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

        // methods (private)
        private static byte[] DecryptGcm(byte[] blob, byte[] key, byte[]? associatedData = null) {
            // validate the binary AES-256-GCM envelope before invoking the cipher
            if (key.Length != AesKeyLength) {
                throw new Exception("Unable to decrypt value: AES-256-GCM key must be 32 bytes.");
            }
            if (blob.Length < GcmNonceLength + GcmTagLength) {
                throw new Exception("Unable to decrypt value: token is truncated.");
            }

            var nonce = new byte[GcmNonceLength];
            Buffer.BlockCopy(blob, 0, nonce, 0, nonce.Length);
            var ciphertextWithTag = new byte[blob.Length - nonce.Length];
            Buffer.BlockCopy(blob, nonce.Length, ciphertextWithTag, 0, ciphertextWithTag.Length);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), GcmTagLength * 8, nonce, associatedData);
            cipher.Init(false, parameters);

            var output = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
            var length = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, output, 0);
            length += cipher.DoFinal(output, length);
            if (length == output.Length) {
                return output;
            }

            var plaintext = new byte[length];
            Buffer.BlockCopy(output, 0, plaintext, 0, length);
            return plaintext;
        }
        private byte[] ResolveV2Key(string? password, JsonElement meta, string saltHex, string path) {
            // validate v2 metadata completely before performing the expensive derivation
            if (!Regex.IsMatch(saltHex, @"\A[0-9a-fA-F]{32}\z", RegexOptions.CultureInvariant)) {
                throw new Exception("Unable to load vault meta: v2 salt must be 16 hexadecimal bytes.");
            }
            if (!meta.TryGetProperty("encrypted_key", out var encryptedKeyNode) || encryptedKeyNode.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(encryptedKeyNode.GetString())) {
                throw new Exception("Unable to load vault meta: v2 encrypted_key is missing.");
            }

            var wrappedDek = DecodeBase64Url(encryptedKeyNode.GetString()!, "v2 encrypted_key");
            if (wrappedDek.Length != GcmNonceLength + AesKeyLength + GcmTagLength) {
                throw new Exception("Unable to load vault meta: v2 encrypted_key must be 60 bytes.");
            }

            var kek = DeriveKey(password, saltHex, path, 2);
            try {
                var dek = DecryptGcm(wrappedDek, kek, V2DekAssociatedData);
                if (dek.Length != AesKeyLength) {
                    throw new Exception("Unable to load vault meta: unwrapped v2 DEK must be 32 bytes.");
                }
                return dek;
            } catch (InvalidCipherTextException error) {
                throw new Exception("Unable to validate password: invalid password or encrypted key.", error);
            }
        }
        private static byte[] DecodeBase64Url(string value, string description) {
            // accept canonical padded or unpadded base64url while rejecting other alphabets
            if (!Regex.IsMatch(value, @"\A[A-Za-z0-9_-]*={0,2}\z", RegexOptions.CultureInvariant)) {
                throw new Exception($"Unable to load vault meta: {description} has invalid base64url encoding.");
            }

            var padded = value;
            while (padded.Length % 4 != 0) {
                padded += "=";
            }
            try {
                return DProjects.Utils.Base64Utils.FromBase64UrlSafe(padded);
            } catch (FormatException error) {
                throw new Exception($"Unable to load vault meta: {description} has invalid base64url encoding.", error);
            }
        }
    }

}
