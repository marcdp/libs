using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Test {

    public class XVaultCompatibilityTests {

        // consts
        private const string Password = "correct horse battery staple";
        private const string WrongPassword = "distinctive-wrong-password";
        private const string SecretValue = "json-secret";
        private const string CanonicalV2Password = "fixture-v2-password";
        private const string RekeyOldPassword = "fixture-v2-old-password";
        private const string RekeyNewPassword = "fixture-v2-new-password";

        // methods
        [Theory]
        [InlineData("compatibility.json")]
        [InlineData("compatibility.jsonc")]
        public void DecryptJsonFixtureMatchesExpectedSemantics(string fixtureName) {
            // decrypt a canonical fixture and compare its JSON data
            var actual = new XVault(FixturePath(fixtureName), Password).Decrypt();
            var expected = File.ReadAllText(FixturePath(fixtureName + ".expected"));
            Assert.Equal(ParseJson(expected).ToJsonString(), ParseJson(actual).ToJsonString());
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptJsoncPreservesApplicationComment() {
            // verify JSONC content survives metadata removal and decryption
            var actual = new XVault(FixturePath("compatibility.jsonc"), Password).Decrypt();
            Assert.Contains("// retained application comment", actual, StringComparison.Ordinal);
        }
        [Theory]
        [InlineData("compatibility.json", "json-secret", "json-api-key")]
        [InlineData("compatibility.jsonc", "jsonc-secret", "jsonc-api-key")]
        public void RegisterJsonMakesDecryptedValuesAvailable(string fixtureName, string passwordValue, string apiKeyValue) {
            // register decrypted JSON or JSONC as application configuration
            var configuration = new ConfigurationManager();
            new XVault(FixturePath(fixtureName), Password).Register(configuration);
            Assert.Equal("public-user", configuration["username"]);
            Assert.Equal(passwordValue, configuration["password"]);
            Assert.Equal(apiKeyValue, configuration["nested:api_key"]);
            Assert.Null(configuration["_xvault"]);
        }
        [Fact]
        public void DecryptYamlFixtureMatchesExpectedDocument() {
            // decrypt canonical YAML while retaining nested and plain values
            var actual = new XVault(FixturePath("compatibility.yaml"), Password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath("compatibility.yaml.expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void RegisterYamlMakesDecryptedValuesAvailable() {
            // register decrypted YAML as application configuration
            var configuration = new ConfigurationManager();
            new XVault(FixturePath("compatibility.yaml"), Password).Register(configuration);
            Assert.Equal("public-user", configuration["username"]);
            Assert.Equal("yaml-secret", configuration["password"]);
            Assert.Equal("yaml-api-key", configuration["nested:api_key"]);
            Assert.Null(configuration["_xvault"]);
        }
        [Fact]
        public void DecryptEnvFixturePreservesNonMetadataContent() {
            // decrypt canonical ENV without discarding comments or blank lines
            var actual = new XVault(FixturePath("compatibility.env"), Password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath("compatibility.env.expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptMarkdownFixturePreservesBody() {
            // decrypt canonical Markdown while removing XVault front matter
            var actual = new XVault(FixturePath("compatibility.md"), Password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath("compatibility.md.expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptXmlReaderFixtureProducesValidExpectedXml() {
            // verify the existing XML reader envelope with canonical crypto tokens
            var actual = new XVault(FixturePath("compatibility.xml"), Password).Decrypt();
            var expected = File.ReadAllText(FixturePath("compatibility.xml.expected"));
            Assert.True(XNode.DeepEquals(XDocument.Parse(expected), XDocument.Parse(actual)));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Theory]
        [InlineData(".json")]
        [InlineData(".jsonc")]
        [InlineData(".yaml")]
        [InlineData(".yml")]
        [InlineData(".xml")]
        [InlineData(".env")]
        [InlineData(".md")]
        [InlineData(".markdown")]
        public void ConstructorRecognizesSupportedExtensionAliases(string extension) {
            // isolate extension selection from decryption behavior
            using var file = TemporaryFile.Create(extension, "not decrypted by this test");
            _ = new XVault(file.Path, Password);
        }
        [Fact]
        public void ConstructorRejectsUnknownExtensionExplicitly() {
            // reject formats outside the reader contract
            using var file = TemporaryFile.Create(".txt", "unsupported");
            Assert.Throws<NotSupportedException>(() => new XVault(file.Path, Password));
        }
        [Theory]
        [InlineData("compatibility.env")]
        [InlineData("compatibility.xml")]
        [InlineData("compatibility.md")]
        public void RegisterRejectsDocumentFormatsWithoutConfigurationSemantics(string fixtureName) {
            // expose deliberate registration capability differences
            var configuration = new ConfigurationManager();
            Assert.Throws<NotSupportedException>(() => new XVault(FixturePath(fixtureName), Password).Register(configuration));
        }
        [Fact]
        public void DecryptRejectsMissingMetadata() {
            // require explicit XVault metadata at the trust boundary
            using var file = TemporaryFile.Create(".json", "{\"username\":\"public-user\"}");
            var error = Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("_xvault", error.Message, StringComparison.Ordinal);
        }
        [Theory]
        [InlineData("vault:not-xvault")]
        [InlineData("xvault:%%%")]
        [InlineData("xvault:ew==")]
        public void DecryptRejectsMalformedMetadata(string metadata) {
            // reject invalid prefixes, encodings, and metadata JSON
            using var file = TemporaryJsonWithMetadata(metadata);
            Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
        }
        [Fact]
        public void DecryptRejectsMetadataWithoutSalt() {
            // reject incomplete key-derivation metadata
            using var file = TemporaryJsonWithMetadata(EncodeMetadata("{\"crypto_version\":1,\"check\":null}"));
            var error = Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("salt", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptRejectsInvalidCryptoFields() {
            // reject metadata whose version has the wrong JSON type
            const string metadata = "{\"crypto_version\":\"one\",\"salt\":\"00112233445566778899aabbccddeeff\",\"check\":null}";
            using var file = TemporaryJsonWithMetadata(EncodeMetadata(metadata));
            Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
        }
        [Fact]
        public void DecryptRejectsUnsupportedCryptoVersion() {
            // reject format versions the compatibility reader does not implement
            const string metadata = "{\"crypto_version\":99,\"salt\":\"00112233445566778899aabbccddeeff\",\"check\":null}";
            using var file = TemporaryJsonWithMetadata(EncodeMetadata(metadata));
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("unsupported crypto version 99", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptAcceptsCanonicalMetadataWithoutCheck() {
            // preserve canonical first-use behavior when the check token is absent
            var text = File.ReadAllText(FixturePath("compatibility.json"));
            text = ReplaceMetadata(text, EncodeMetadata("{\"crypto_version\":1,\"salt\":\"00112233445566778899aabbccddeeff\",\"check\":null}"));
            using var file = TemporaryFile.Create(".json", text);
            Assert.Equal(SecretValue, ParseJson(new XVault(file.Path, Password).Decrypt())["password"]!.GetValue<string>());
        }
        [Fact]
        public void DecryptRejectsInvalidPasswordCheckEncoding() {
            // reject malformed password-validation data before processing document secrets
            var text = File.ReadAllText(FixturePath("compatibility.json"));
            text = ReplaceMetadata(text, EncodeMetadata("{\"crypto_version\":1,\"salt\":\"00112233445566778899aabbccddeeff\",\"check\":\"%%%\"}"));
            using var file = TemporaryFile.Create(".json", text);
            Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
        }
        [Fact]
        public void DecryptRejectsWrongPasswordWithoutLeakingInputs() {
            // establish safe deterministic password-validation failure
            var error = Assert.Throws<Exception>(() => new XVault(FixturePath("compatibility.json"), WrongPassword).Decrypt());
            Assert.Contains("invalid password", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(WrongPassword, error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(SecretValue, error.ToString(), StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptRejectsTruncatedEncryptedToken() {
            // reject a blob shorter than nonce plus authentication tag
            using var file = MutatedJsonToken(_ => "AAECAwQ");
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("truncated", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptRejectsTamperedCiphertextWithoutLeakingPlaintext() {
            // prove authenticated decryption rejects a modified tag
            using var file = MutatedJsonToken(TamperToken);
            var error = Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.DoesNotContain(Password, error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(SecretValue, error.ToString(), StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptRejectsInvalidTokenEncoding() {
            // reject a token that is not base64url data
            using var file = MutatedJsonToken(_ => "%%%invalid%%%");
            Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
        }
        [Theory]
        [InlineData("compatibility-v2.json")]
        [InlineData("compatibility-v2.jsonc")]
        public void DecryptV2JsonFixtureMatchesExpectedSemantics(string fixtureName) {
            // decrypt canonical v2 JSON and JSONC output while removing metadata
            var actual = new XVault(FixturePath(fixtureName), Password).Decrypt();
            var expected = File.ReadAllText(FixturePath(fixtureName + ".expected"));
            Assert.Equal(ParseJson(expected).ToJsonString(), ParseJson(actual).ToJsonString());
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptV2JsoncPreservesApplicationComment() {
            // retain non-XVault JSONC comments through v2 decryption
            var actual = new XVault(FixturePath("compatibility-v2.jsonc"), Password).Decrypt();
            Assert.Contains("// retained v2 application comment", actual, StringComparison.Ordinal);
        }
        [Theory]
        [InlineData("compatibility-v2.json", "json-v2-secret", "json-v2-api-key")]
        [InlineData("compatibility-v2.jsonc", "jsonc-v2-secret", "jsonc-v2-api-key")]
        public void RegisterV2JsonMakesDecryptedValuesAvailable(string fixtureName, string passwordValue, string apiKeyValue) {
            // register v2 JSON data without exposing metadata
            var configuration = new ConfigurationManager();
            new XVault(FixturePath(fixtureName), Password).Register(configuration);
            Assert.Equal("public-user", configuration["username"]);
            Assert.Equal(passwordValue, configuration["password"]);
            Assert.Equal(apiKeyValue, configuration["nested:api_key"]);
            Assert.Null(configuration["_xvault"]);
        }
        [Fact]
        public void DecryptV2JsonPreservesUnicodeSecret() {
            // verify UTF-8 plaintext returned by the shared binary GCM path
            var actual = ParseJson(new XVault(FixturePath("compatibility-v2.json"), Password).Decrypt());
            Assert.Equal("Héllo 密码 🔐", actual["nested"]!["unicode"]!.GetValue<string>());
        }
        [Fact]
        public void DecryptAndRegisterV2YamlFixture() {
            // verify document and configuration semantics for canonical v2 YAML output
            var actual = new XVault(FixturePath("compatibility-v2.yaml"), Password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath("compatibility-v2.yaml.expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);

            var configuration = new ConfigurationManager();
            new XVault(FixturePath("compatibility-v2.yaml"), Password).Register(configuration);
            Assert.Equal("public-user", configuration["username"]);
            Assert.Equal("yaml-v2-secret", configuration["password"]);
            Assert.Equal("yaml-v2-api-key", configuration["nested:api_key"]);
            Assert.Null(configuration["_xvault"]);
        }
        [Theory]
        [InlineData("compatibility-v2.env", "correct horse battery staple")]
        [InlineData("canonical-v2.env", "fixture-v2-password")]
        public void DecryptV2EnvFixtureMatchesExpectedDocument(string fixtureName, string password) {
            // verify synthetic formats and the canonical fixed Argon2 vector
            var actual = new XVault(FixturePath(fixtureName), password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath(fixtureName + ".expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptV2MarkdownFixtureMatchesExpectedDocument() {
            // decrypt whole and wrapped v2 markers while preserving the Markdown body
            var actual = new XVault(FixturePath("compatibility-v2.md"), Password).Decrypt();
            Assert.Equal(NormalizeLines(File.ReadAllText(FixturePath("compatibility-v2.md.expected"))), NormalizeLines(actual));
            Assert.DoesNotContain("_xvault", actual, StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptV2RejectsWrongPasswordWithoutLeakingInputs() {
            // authenticate the wrapped DEK before processing any secret token
            var error = Assert.Throws<Exception>(() => new XVault(FixturePath("canonical-v2.env"), WrongPassword).Decrypt());
            Assert.Contains("invalid password or encrypted key", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(WrongPassword, error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("fixture-v2-secret", error.ToString(), StringComparison.Ordinal);
        }
        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        [InlineData(59)]
        public void DecryptV2RejectsTamperedWrappedDek(int byteIndex) {
            // reject changes to the wrapping nonce, ciphertext, or authentication tag
            using var file = MutatedV2Metadata(meta => {
                var blob = DecodeBase64Url(meta["encrypted_key"]!.GetValue<string>());
                blob[byteIndex] ^= 1;
                meta["encrypted_key"] = EncodeBase64Url(blob);
            });
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("invalid password or encrypted key", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptV2RejectsMissingEncryptedKey() {
            // require the v2 wrapping envelope rather than generating replacement state
            using var file = MutatedV2Metadata(meta => meta.Remove("encrypted_key"));
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("encrypted_key", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Theory]
        [InlineData("%%%invalid%%%")]
        [InlineData("AAECAwQ")]
        public void DecryptV2RejectsInvalidOrTruncatedEncryptedKey(string encryptedKey) {
            // reject malformed wrapped-key encodings and lengths before derivation
            using var file = MutatedV2Metadata(meta => meta["encrypted_key"] = encryptedKey);
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("encrypted_key", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptV2RejectsIncorrectDekPayloadLength() {
            // require the wrapping envelope to contain exactly one 32-byte AES key
            var kek = Convert.FromHexString("affb42bf079a283dc71abb6b0ece1984c792b19d1c86320e5e303aa9bb2858ad");
            var nonce = new byte[12];
            var plaintext = new byte[31];
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(kek, tag.Length)) {
                aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes("xvault:v2:dek"));
            }
            var blob = nonce.Concat(ciphertext).Concat(tag).ToArray();
            var metadata = new JsonObject {
                ["crypto_version"] = 2,
                ["salt"] = "00112233445566778899aabbccddeeff",
                ["encrypted_key"] = EncodeBase64Url(blob),
            };
            using var file = TemporaryJsonWithMetadata(EncodeMetadata(metadata.ToJsonString()));
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, CanonicalV2Password).Decrypt());
            Assert.Contains("60 bytes", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Theory]
        [InlineData("0011")]
        [InlineData("00112233445566778899aabbccddeefg")]
        public void DecryptV2RejectsInvalidSalt(string salt) {
            // require the canonical 16-byte hexadecimal v2 salt
            using var file = MutatedV2Metadata(meta => meta["salt"] = salt);
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("salt", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        [Fact]
        public void DecryptV2RejectsTamperedSecretWithoutLeakingPlaintext() {
            // authenticate secret ciphertext with the unwrapped DEK
            using var file = MutatedV2JsonToken(TamperToken);
            var error = Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.DoesNotContain(Password, error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("json-v2-secret", error.ToString(), StringComparison.Ordinal);
        }
        [Fact]
        public void DecryptV2RejectsInvalidSecretEncoding() {
            // reject malformed secret payloads after successful DEK authentication
            using var file = MutatedV2JsonToken(_ => "%%%invalid%%%");
            Assert.ThrowsAny<Exception>(() => new XVault(file.Path, Password).Decrypt());
        }
        [Fact]
        public void DecryptV2PasswordRotationUsesNewPasswordAndPreservesSecretCiphertext() {
            // verify canonical CryptoV2 rotation rewraps one DEK without changing values
            var before = File.ReadAllText(FixturePath("rekey-before-v2.env"));
            var after = File.ReadAllText(FixturePath("rekey-after-v2.env"));
            Assert.Equal(ExtractEnvSecretToken(before), ExtractEnvSecretToken(after));
            var beforeMeta = ParseMetadata(ExtractEnvMetadata(before));
            var afterMeta = ParseMetadata(ExtractEnvMetadata(after));
            Assert.NotEqual(beforeMeta["salt"]!.GetValue<string>(), afterMeta["salt"]!.GetValue<string>());
            Assert.NotEqual(beforeMeta["encrypted_key"]!.GetValue<string>(), afterMeta["encrypted_key"]!.GetValue<string>());
            Assert.Contains("rekey-secret", new XVault(FixturePath("rekey-before-v2.env"), RekeyOldPassword).Decrypt(), StringComparison.Ordinal);
            Assert.Contains("rekey-secret", new XVault(FixturePath("rekey-after-v2.env"), RekeyNewPassword).Decrypt(), StringComparison.Ordinal);
            Assert.Throws<Exception>(() => new XVault(FixturePath("rekey-after-v2.env"), RekeyOldPassword).Decrypt());
        }
        [Fact]
        public void KeyringIdentityIsVersionSpecific() {
            // preserve v1 identity while isolating cached v2 KEKs
            var path = FixturePath("canonical-v2.env");
            var fileUri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
            Assert.Equal("xvault:" + fileUri, Handler.GetKeyringName(path, 1));
            Assert.Equal("xvault:v2:" + fileUri, Handler.GetKeyringName(path, 2));
        }

        // methods (private)
        private static string FixturePath(string name) {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }
        private static JsonNode ParseJson(string value) {
            return JsonNode.Parse(value,
                documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
        }
        private static string NormalizeLines(string value) {
            return value.Replace("\r\n", "\n").TrimEnd('\n', '\r');
        }
        private static TemporaryFile TemporaryJsonWithMetadata(string metadata) {
            return TemporaryFile.Create(".json", "{\"_xvault\":\"" + metadata + "\",\"value\":\"plain\"}");
        }
        private static string EncodeMetadata(string json) {
            return "xvault:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).Replace('+', '-').Replace('/', '_');
        }
        private static string ReplaceMetadata(string text, string metadata) {
            const string pattern = "(?<=\\\"_xvault\\\"\\s*:\\s*\\\")[^\\\"]+";
            return Regex.Replace(text, pattern, metadata, RegexOptions.CultureInvariant);
        }
        private static TemporaryFile MutatedJsonToken(Func<string, string> mutate) {
            var text = File.ReadAllText(FixturePath("compatibility.json"));
            text = Regex.Replace(text, "(?<=\\\"password\\\"\\s*:\\s*\\\"enc:)[^\\\"]+", match => mutate(match.Value),
                RegexOptions.CultureInvariant);
            return TemporaryFile.Create(".json", text);
        }
        private static string TamperToken(string token) {
            var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/'));
            bytes[bytes.Length - 1] ^= 1;
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
        }
        private static TemporaryFile MutatedV2Metadata(Action<JsonObject> mutate) {
            var text = File.ReadAllText(FixturePath("compatibility-v2.json"));
            var match = Regex.Match(text, "(?<=\\\"_xvault\\\"\\s*:\\s*\\\")[^\\\"]+", RegexOptions.CultureInvariant);
            var meta = ParseMetadata(match.Value);
            mutate(meta);
            return TemporaryFile.Create(".json", ReplaceMetadata(text, EncodeMetadata(meta.ToJsonString())));
        }
        private static TemporaryFile MutatedV2JsonToken(Func<string, string> mutate) {
            var text = File.ReadAllText(FixturePath("compatibility-v2.json"));
            text = Regex.Replace(text, "(?<=\\\"password\\\"\\s*:\\s*\\\"enc:)[^\\\"]+", match => mutate(match.Value), RegexOptions.CultureInvariant);
            return TemporaryFile.Create(".json", text);
        }
        private static JsonObject ParseMetadata(string rawMetadata) {
            var encoded = rawMetadata.Substring("xvault:".Length);
            return JsonNode.Parse(Encoding.UTF8.GetString(DecodeBase64Url(encoded)))!.AsObject();
        }
        private static byte[] DecodeBase64Url(string value) {
            while (value.Length % 4 != 0) {
                value += "=";
            }
            return Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/'));
        }
        private static string EncodeBase64Url(byte[] value) {
            return Convert.ToBase64String(value).Replace('+', '-').Replace('/', '_');
        }
        private static string ExtractEnvMetadata(string text) {
            return Regex.Match(text, "(?m)^_xvault=(.+)$", RegexOptions.CultureInvariant).Groups[1].Value.Trim();
        }
        private static string ExtractEnvSecretToken(string text) {
            return Regex.Match(text, "(?m)^SECRET=enc:(.+)$", RegexOptions.CultureInvariant).Groups[1].Value.Trim();
        }

        private sealed class TemporaryFile : IDisposable {

            // props
            public string Path { get; }

            // ctor
            private TemporaryFile(string path) {
                Path = path;
            }

            // methods
            public static TemporaryFile Create(string extension, string contents) {
                var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dprojects-xvault-{Guid.NewGuid():N}{extension}");
                File.WriteAllText(path, contents);
                return new TemporaryFile(path);
            }
            public void Dispose() {
                File.Delete(Path);
            }
        }
    }
}
