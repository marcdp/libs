using System.Net;
using System.Text.RegularExpressions;
using DProjects.XShell.Services;

namespace DProjects.XShell.Test {

    public sealed class BootstrapFilesBuilderTests {

        // methods
        [Fact]
        public void Build_AppParams_RoundTripsSpecialCharactersWithoutInjectingMarkup() {
            var parameters = new Dictionary<string, string> {
                ["a"] = "123",
                ["ampersand"] = "x&y",
                ["equals"] = "x=y",
                ["plus"] = "x+y",
                ["space"] = "x y",
                ["percent"] = "50%",
                ["quote"] = "\"quoted\"",
                ["unicode"] = "España ✓",
                ["España ✓"] = "value",
                ["a&b"] = "value",
                ["injection"] = "value\" data-injected=\"true"
            };

            var index = BuildIndex(parameters);
            var appParamsTag = GetAppParamsTag(index);
            var serializedParams = WebUtility.HtmlDecode(GetContentAttribute(appParamsTag));

            Assert.Equal(parameters, ParseUrlSearchParams(serializedParams));
            Assert.Contains("&amp;", appParamsTag);
            Assert.Equal(4, appParamsTag.Count(character => character == '\"'));
            Assert.DoesNotContain("data-injected=\"true\"", appParamsTag);
        }
        [Fact]
        public void Build_AppParams_EmptyParamsProducesEmptyContentAttribute() {
            var index = BuildIndex([]);

            Assert.Equal("", GetContentAttribute(GetAppParamsTag(index)));
        }

        // methods (private)
        private static string BuildIndex(Dictionary<string, string> parameters) {
            return new BootstrapFilesBuilder().Build(new Extensions.Configuration {
                App = new Extensions.AppConfig { Params = parameters }
            }, "Development")["/index.html"];
        }
        private static string GetAppParamsTag(string index) {
            return Regex.Match(index, "<meta name=\"xshell:app.params\"[^>]*>").Value;
        }
        private static string GetContentAttribute(string tag) {
            return Regex.Match(tag, "content=\"(?<value>[^\"]*)\"").Groups["value"].Value;
        }
        private static Dictionary<string, string> ParseUrlSearchParams(string serializedParams) {
            return serializedParams.Split('&', StringSplitOptions.RemoveEmptyEntries).ToDictionary(
                pair => DecodeUrlSearchParam(pair.Split('=', 2)[0]),
                pair => DecodeUrlSearchParam(pair.Split('=', 2).ElementAtOrDefault(1) ?? ""));
        }
        private static string DecodeUrlSearchParam(string value) {
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }
    }
}
