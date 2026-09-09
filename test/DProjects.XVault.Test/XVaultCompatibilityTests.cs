using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault.Test {

    public class XVaultCompatibilityTests {

        // consts
        private const string Password = "correct horse battery staple";
        private const string WrongPassword = "distinctive-wrong-password";
        private const string SecretValue = "json-secret";

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
            const string metadata = "{\"crypto_version\":2,\"salt\":\"00112233445566778899aabbccddeeff\",\"check\":null}";
            using var file = TemporaryJsonWithMetadata(EncodeMetadata(metadata));
            var error = Assert.Throws<Exception>(() => new XVault(file.Path, Password).Decrypt());
            Assert.Contains("unsupported crypto version 2", error.Message, StringComparison.OrdinalIgnoreCase);
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

        // methods (private)
        private static string FixturePath(string name) {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        }
        private static JsonNode ParseJson(string value) {
            return JsonNode.Parse(value, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
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
            text = Regex.Replace(text, "(?<=\\\"password\\\"\\s*:\\s*\\\"enc:)[^\\\"]+", match => mutate(match.Value), RegexOptions.CultureInvariant);
            return TemporaryFile.Create(".json", text);
        }
        private static string TamperToken(string token) {
            var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/'));
            bytes[bytes.Length - 1] ^= 1;
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
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
