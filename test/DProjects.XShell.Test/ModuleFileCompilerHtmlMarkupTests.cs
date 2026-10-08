using DProjects.XShell.Services;
using System.Reflection;
using System.Text.RegularExpressions;

using Xunit;

namespace DProjects.XShell.Test {

    public sealed class ModuleFileCompilerHtmlMarkupTests {

        // methods
        [Fact]
        public void Compile_UrlAttributes_RewritesValuesAndPreservesMarkup() {
            var html = "<img SRC = \"./image.png\" poster='../video.jpg'><a href=./details.html>link</a>" +
                "<form action=\"../save?v=2#result\"><button formaction='./confirm'>save</button></form>";

            var result = CompileHtml(html);

            Assert.Equal("<img SRC = \"/pages/users/image.png\" poster='../video.jpg'><a href=/pages/users/details.html>link</a>" +
                "<form action=\"/pages/save?v=2#result\"><button formaction='/pages/users/confirm'>save</button></form>", result.Content);
        }
        [Theory]
        [InlineData("a", "href")]
        [InlineData("area", "href")]
        [InlineData("form", "action")]
        [InlineData("button", "formaction")]
        [InlineData("x-page", "src")]
        [InlineData("x-anchor", "href")]
        [InlineData("img", "src")]
        [InlineData("img", "srcset")]
        [InlineData("source", "src")]
        [InlineData("source", "srcset")]
        [InlineData("link", "href")]
        [InlineData("script", "src")]
        [InlineData("iframe", "src")]
        [InlineData("video", "poster")]
        [InlineData("video", "src")]
        [InlineData("audio", "src")]
        [InlineData("embed", "src")]
        [InlineData("object", "data")]
        [InlineData("input", "src")]
        [InlineData("track", "src")]
        public void Compile_SupportedBuiltInUrlRules_Normalize(string element, string attribute) {
            var type = element == "input" ? " type='image'" : "";
            var html = $"<{element} {attribute}='./asset.png?v=1#part'{type}></{element}>";

            Assert.Equal($"<{element} {attribute}='/pages/users/asset.png?v=1#part'{type}></{element}>", CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_BuiltInRules_MatchRuntimeTable() {
            // compare the two language-specific built-in tables so additions cannot silently drift
            var projectDirectory = typeof(ModuleFileCompilerHtml).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "ProjectDirectory").Value!;
            var runtime = File.ReadAllText(Path.Combine(projectDirectory, "Resources", "DProjects.XShell", "xshell", "utils", "html.js"));
            var runtimeRules = Regex.Matches(runtime, "\\{ selector: \"([^\"]+)\", attr: \"([^\"]+)\", type: \"[^\"]+\" \\}")
                .Select(match => (Selector: match.Groups[1].Value, Attribute: match.Groups[2].Value)).ToArray();

            Assert.Equal(runtimeRules, ModuleFileCompilerHtml.UrlRules);
        }
        [Theory]
        [InlineData("<svg><use href='./icons.svg#edit'></use></svg>")]
        [InlineData("<div href='./x' src='./x' poster='./x' action='./x' formaction='./x'></div>")]
        [InlineData("<input type='text' src='./x'>")]
        [InlineData("<custom-element href='./x' srcset='./x 1x'></custom-element>")]
        [InlineData("<div data-value='app:/foo' href='url:./x'></div>")]
        public void Compile_UnownedUrlLookingAttributes_RemainUntouched(string html) {
            Assert.Equal(html, CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_InputImageSelector_IsCaseInsensitiveAndIndependentOfAttributeOrder() {
            Assert.Equal("<INPUT SRC='/pages/users/a.png' TYPE='IMAGE'><A HREF='/pages/users/page'></A>",
                CompileHtml("<INPUT SRC='./a.png' TYPE='IMAGE'><A HREF='./page'></A>").Content);
            Assert.Equal("<INPUT TYPE='TEXT' SRC='./a.png'>", CompileHtml("<INPUT TYPE='TEXT' SRC='./a.png'>").Content);
        }
        [Fact]
        public void Compile_OnlyImgAndSourceSrcset_RewriteCandidates() {
            var html = "<img srcset='./a.png 1x, ./b.png 2x'><source srcset='./c.png 320w'><div srcset='./d.png 1x'></div>";
            Assert.Equal("<img srcset='/pages/users/a.png 1x, /pages/users/b.png 2x'><source srcset='/pages/users/c.png 320w'><div srcset='./d.png 1x'></div>",
                CompileHtml(html).Content);
        }
        [Theory]
        [InlineData("/images/user.png")]
        [InlineData("https://example.com/user.png")]
        [InlineData("//cdn.example.com/user.png")]
        [InlineData("data:image/png;base64,AAAA")]
        [InlineData("blob:https://example.com/id")]
        [InlineData("mailto:user@example.com")]
        [InlineData("tel:+34123456789")]
        [InlineData("file:///tmp/image.png")]
        [InlineData("javascript:void(0)")]
        [InlineData("#details")]
        public void Compile_NonRelativeUrl_LeavesValueUnchanged(string url) {
            var html = $"<a href=\"{url}\">link</a>";

            Assert.Equal(html, CompileHtml(html).Content);
        }
        [Theory]
        [InlineData("<img src=\"url:./image.png\">")]
        [InlineData("<a href='url:../page.html'>link</a>")]
        [InlineData("<video poster=\"url:./poster.png\"></video>")]
        [InlineData("<form action=\"url:./submit\"></form>")]
        [InlineData("<button formaction=\"url:./submit\">go</button>")]
        [InlineData("<img srcset=\"small.png 1x, url:./large.png 2x\">")]
        [InlineData("<div style=\"background:url(url:./image.png)\"></div>")]
        [InlineData("<img src=\"app:/image.png\">")]
        [InlineData("<a href='APP:/page.html'>link</a>")]
        [InlineData("<img srcset=\"small.png 1x, app:/large.png 2x\">")]
        [InlineData("<div style=\"background:url(APP:/image.png)\"></div>")]
        public void Compile_ConfigurationOnlyUrlScheme_RejectsResourceReferences(string html) {
            var exception = Assert.Throws<InvalidOperationException>(() => CompileHtml(html));

            Assert.Contains("scheme is not supported", exception.Message);
        }
        [Fact]
        public void Compile_UrlSchemeTextAndUnrelatedAttributes_RemainUntouched() {
            var html = "<p title=\"url:./literal\">url:./plain text</p><script>const text = 'url:./script';</script>";

            Assert.Equal(html, CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_Srcset_RewritesCandidatesAndPreservesDescriptorsAndDataUrl() {
            var html = "<img srcset=\"./small.jpg 1x, ../large.jpg 2x, ./wide.jpg 640w, data:image/png;base64,AAAA 320w\">";

            var result = CompileHtml(html);

            Assert.Equal("<img srcset=\"/pages/users/small.jpg 1x, /pages/large.jpg 2x, /pages/users/wide.jpg 640w, data:image/png;base64,AAAA 320w\">", result.Content);
        }
        [Fact]
        public void Compile_CommentsTextAndQuotedGreaterThan_ChangesOnlyRealAttribute() {
            var html = "<!-- <img src=\"./comment.png\"> --><p title=\"1 > 0\">src=\"./text.png\"</p><IMG SrC='./real.png'>";

            var result = CompileHtml(html);

            Assert.Equal("<!-- <img src=\"./comment.png\"> --><p title=\"1 > 0\">src=\"./text.png\"</p><IMG SrC='/pages/users/real.png'>", result.Content);
        }
        [Fact]
        public void Compile_InlineStyles_UsesCssCompilerAndPreservesOuterQuotes() {
            var html = "<div style=\"background: url('../images/double.png');\"></div>" +
                "<div style='background:url(\"./single.png\")'></div>" +
                "<div style=\"background:url(&quot;./encoded.png&quot;); content: '&amp;'\"></div>";

            var result = CompileHtml(html);

            Assert.Equal("<div style=\"background: url('/pages/images/double.png');\"></div>" +
                "<div style='background:url(\"/pages/users/single.png\")'></div>" +
                "<div style=\"background:url(&quot;/pages/users/encoded.png&quot;); content: '&amp;'\"></div>", result.Content);
        }
        [Fact]
        public void Compile_StyleElement_LeavesCompleteElementUnchanged() {
            var html = "<style data-example=\"./unchanged.css\">.x { background: url(\"./image.png\"); } /* <img src='./also.png'> */</style>";

            Assert.Equal(html, CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_ScriptRegression_LeavesCompleteScriptsUnchangedAndProcessesFollowingMarkup() {
            var html = """
                <script data-source="./unchanged.js">
                    const value = '<img src="./should-not-change.png">';
                    const url = "./other.js";
                </script>
                <script type="module">
                    import x from "./module.js";
                </script>

                <div style="background:url('./should-change.png')"></div>
                <img src="./should-change-too.png">
                """;
            var expected = html.Replace("./should-change.png", "/pages/users/should-change.png", StringComparison.Ordinal)
                .Replace("./should-change-too.png", "/pages/users/should-change-too.png", StringComparison.Ordinal);

            Assert.Equal(expected, CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_CaseInsensitiveRawTextTags_LeavesBodiesUnchanged() {
            var html = "<SCRIPT>const html = '<img src=\"./image.png\">';</ScRiPt><STYLE>.x{background:url(./x.png)}</sTyLe>";

            Assert.Equal(html, CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_GenericHtml_AllowsScriptsAndStylesAndRewritesScriptSource() {
            var html = "<script src=\"./module.js\"></script><style>.x{background:url(./image.png)}</style>";

            Assert.Equal("<script src=\"/pages/users/module.js\"></script><style>.x{background:url(./image.png)}</style>", CompileHtml(html).Content);
        }
        [Fact]
        public void Compile_TraversalAboveModuleRoot_Throws() {
            Assert.Throws<InvalidOperationException>(() => CompileHtml("<img src=\"../../../outside.png\">"));
            Assert.Throws<InvalidOperationException>(() => CompileHtml("<img src=\"/../outside.png\">"));
        }
        [Fact]
        public void Compile_CssAndHtml_UseIdenticalUrlNormalization() {
            var context = CreateContext();
            var html = new ModuleFileCompilerHtml().Compile(context, "<img src=\"../images/user.png?v=2#photo\">");
            var css = new ModuleFileCompilerCss().Compile(context, ".x { background: url(\"../images/user.png?v=2#photo\"); }");

            Assert.Equal("<img src=\"/pages/images/user.png?v=2#photo\">", html.Content);
            Assert.Equal(".x { background: url(\"/pages/images/user.png?v=2#photo\"); }", css.Content);
        }

        // methods (private)
        private static ModuleFileCompiler.FileContent CompileHtml(string html) {
            return new ModuleFileCompilerHtml().Compile(CreateContext(), html);
        }
        private static ModuleFileCompilerContext CreateContext() {
            var modulePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "module"));
            return new ModuleFileCompilerContext(new ModuleFileCompiler.Config(), "test", modulePath, Path.Combine(modulePath, "pages", "users", "details.js"));
        }
    }
}
