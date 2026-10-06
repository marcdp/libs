using DProjects.XShell.Services;

using Xunit;

namespace DProjects.XShell.Test {

    public sealed class ModuleFileCompilerCssTests {

        // methods
        [Theory]
        [InlineData("url(image.png)", "url(/styles/image.png)")]
        [InlineData("url(./image.png)", "url(/styles/image.png)")]
        [InlineData("url(image.png?v=1#icon)", "url(/styles/image.png?v=1#icon)")]
        public void Compile_RelativeUrl_RewritesAgainstStylesheetDirectory(string css, string expected) {
            Assert.Equal(expected, Compile(css));
        }
        [Theory]
        [InlineData("url(../image.png)", "url(/styles/image.png)")]
        [InlineData("url(../../image.png)", "url(/image.png)")]
        [InlineData("url(../image.png?v=1#icon)", "url(/styles/image.png?v=1#icon)")]
        public void Compile_ParentSegments_ResolveWithinModuleRoot(string css, string expected) {
            Assert.Equal(expected, Compile(css, "styles/nested/site.css"));
        }
        [Theory]
        [InlineData("url(/images/logo.png)")]
        [InlineData("filter:url(#filter);")]
        [InlineData("url(https://cdn.test/image.png)")]
        [InlineData("url(http://cdn.test/image.png)")]
        [InlineData("url(data:image/png;base64,AAAA)")]
        [InlineData("url(blob:https://example.test/id)")]
        [InlineData("url()")]
        [InlineData("url(?v=1)")]
        [InlineData("url(#fragment)")]
        public void Compile_NonRelativeUrl_PreservesReference(string css) {
            Assert.Equal(css, Compile(css));
        }
        [Theory]
        [InlineData("url(\"image.png\")", "url(\"/styles/image.png\")")]
        [InlineData("url('image.png')", "url('/styles/image.png')")]
        [InlineData("url( image.png )", "url( /styles/image.png )")]
        [InlineData("url( \"image.png\" )", "url( \"/styles/image.png\" )")]
        [InlineData("url   (image.png)", "url   (/styles/image.png)")]
        [InlineData("URL(image.png)", "URL(/styles/image.png)")]
        public void Compile_UrlSyntax_PreservesQuotesWhitespaceAndFunctionSpelling(string css, string expected) {
            Assert.Equal(expected, Compile(css));
        }
        [Theory]
        [InlineData("@import \"theme.css\";", "@import \"/styles/theme.css\";")]
        [InlineData("@import 'theme.css';", "@import '/styles/theme.css';")]
        [InlineData("@import url(theme.css);", "@import url(/styles/theme.css);")]
        [InlineData("@import \"print.css\" print;", "@import \"/styles/print.css\" print;")]
        [InlineData("@IMPORT \"theme.css\";", "@IMPORT \"/styles/theme.css\";")]
        public void Compile_Import_RewritesOnlyResource(string css, string expected) {
            Assert.Equal(expected, Compile(css));
        }
        [Fact]
        public void Compile_Comments_PreservesUrlLookingTextAndResumesScanning() {
            const string css = "/* url(ignore.png) */\n.a { background:url(real.png); }";

            Assert.Equal("/* url(ignore.png) */\n.a { background:url(/styles/real.png); }", Compile(css));
        }
        [Fact]
        public void Compile_OrdinaryStrings_PreservesUrlLookingTextAndResumesScanning() {
            const string css = "content:\"url(ignore.png)\"; background:url(real.png);";

            Assert.Equal("content:\"url(ignore.png)\"; background:url(/styles/real.png);", Compile(css));
        }
        [Fact]
        public void Compile_EscapedQuotesInString_PreservesStringAndResumesScanning() {
            const string css = "content:\"escaped \\\" url(ignore.png)\"; background:url(real.png);";

            Assert.Equal("content:\"escaped \\\" url(ignore.png)\"; background:url(/styles/real.png);", Compile(css));
        }
        [Fact]
        public void Compile_EscapedClosingParenthesis_ProcessesCompleteUnquotedUrl() {
            Assert.Equal(@"url(/styles/image\)part.png)", Compile(@"url(image\)part.png)"));
        }
        [Theory]
        [InlineData("url(\"image.png")]
        [InlineData("url(image.png")]
        public void Compile_UnterminatedUrl_PreservesRemainingInput(string css) {
            Assert.Equal(css, Compile(css));
        }
        [Fact]
        public void Compile_TraversalAboveModuleRoot_IdentifiesReferenceAndStylesheet() {
            var exception = Assert.Throws<InvalidOperationException>(() => Compile("url(../../outside.png)"));

            Assert.Contains("../../outside.png", exception.Message);
            Assert.Contains("/styles/site.css", exception.Message);
        }
        [Theory]
        [InlineData("myurl(image.png)")]
        [InlineData("my-url(image.png)")]
        [InlineData("@imported \"theme.css\";")]
        public void Compile_IdentifierSubstrings_AreNotResourceConstructs(string css) {
            Assert.Equal(css, Compile(css));
        }

        // methods (private)
        private static string Compile(string css, string relativePath = "styles/site.css") {
            var modulePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "module"));
            var filePath = Path.Combine(modulePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var context = new ModuleFileCompilerContext(new ModuleFileCompiler.Config(), "test", modulePath, filePath);
            var result = new ModuleFileCompilerCss().Compile(context, css);
            Assert.Equal("text/css", result.ContentType);
            return result.Content;
        }
    }
}
