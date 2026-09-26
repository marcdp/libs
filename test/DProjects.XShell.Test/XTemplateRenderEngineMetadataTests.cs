using System.Diagnostics;
using System.Text.Json;

namespace DProjects.XShell.Test;

public sealed class XTemplateRenderEngineMetadataTests {

    // methods
    [Fact]
    public void BrowserFactoryUsesOnlyCompiledMetadataAndPreservesConfiguredLazyBoundary() {
        var result = ExecuteRuntimeScenario();

        Assert.Equal(new[] { "component:x-lazy", "component:x-shared" }, result.Dependencies);
        Assert.Equal(new[] { "", "footer" }, result.Slots);
        Assert.True(result.DependenciesFrozen);
        Assert.True(result.SlotsFrozen);
        Assert.True(result.RawTemplateWasNotParsed);
        Assert.Contains("artifact with render, dependencies, and slots", result.LegacyRendererError);
        Assert.Equal("#/pages/details", result.NavigationHref);
        Assert.Equal("/app/pages/images/a.png", result.ResourceSrc);
        Assert.Equal("/app/pages/send.png", result.ContextSensitiveSrc);
    }

    // methods (private)
    private static MetadataRuntimeResult ExecuteRuntimeScenario() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "render-engines", "x.js");
        Assert.True(File.Exists(runtimePath), $"The copied browser runtime was not found at '{runtimePath}'.");
        var modulePath = Path.Combine(Path.GetTempPath(), $"xtemplate-metadata-{Guid.NewGuid():N}.mjs");
        try {
            File.WriteAllText(modulePath, CreateJavaScriptModule(runtimePath));
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"XTemplate metadata runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<MetadataRuntimeResult>(output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("XTemplate metadata runtime returned no result.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string CreateJavaScriptModule(string runtimePath) {
        return $$"""
            globalThis.HTMLElement = class {};
            globalThis.CSSStyleSheet = class { replaceSync() {} };
            globalThis.customElements = { get() {}, define() {} };
            globalThis.window = { customElements:globalThis.customElements };
            let rawTemplateWasNotParsed = true;
            globalThis.document = {
                createElement(tag) {
                    if (tag.toLowerCase() === "template") {
                        rawTemplateWasNotParsed = false;
                        throw new Error("Raw XTemplate parsing is forbidden.");
                    }
                    return {
                        tag:tag.toLowerCase(),
                        attrs:{},
                        setAttribute(name, value) { this.attrs[name] = value; },
                        matches(selector) {
                            if (selector === "a" || selector === "img") return this.tag === selector;
                            if (selector === "input[type=image]") return this.tag === "input" && this.attrs.type === "image";
                            return false;
                        }
                    };
                }
            };
            const { default:createRenderEngineFactoryX } = await import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}});
            const { rewriteTemplateAttribute } = await import(new URL("../utils/rewriteDocumentUrls.js", {{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}));
            const templateRenderer = {
                render:() => [],
                dependencies:[
                    { resource:"component:x-lazy", ancestorPaths:[[]] },
                    { resource:"component:x-heavy", ancestorPaths:[["x-lazy"]] },
                    { resource:"component:x-shared", ancestorPaths:[["x-lazy"], []] }
                ],
                slots:["", "footer", "footer"]
            };
            const factory = createRenderEngineFactoryX('<div style="display:none"><slot name="raw"></slot><x-raw></x-raw></div>', { componentLazy:"x-lazy" }, templateRenderer);
            factory.init();
            let legacyRendererError = "";
            try {
                createRenderEngineFactoryX("<x-legacy></x-legacy>", {}, () => []);
            } catch (error) {
                legacyRendererError = error.message;
            }
            const rewriteContext = {
                appBasePath:"/app",
                resourcePath:"/pages/card.js",
                resourceDefinition:{ modulePath:"/module" },
                navigationMode:"hash",
                navigationHashPrefix:"#"
            };
            const navigationHref = rewriteTemplateAttribute("a", { href:"details" }, "href", "details", rewriteContext);
            const resourceSrc = rewriteTemplateAttribute("img", { src:"images/a.png" }, "src", "images/a.png", rewriteContext);
            const contextSensitiveSrc = rewriteTemplateAttribute("input", { type:"image", src:"send.png" }, "src", "send.png", rewriteContext);
            console.log(JSON.stringify({
                dependencies:factory.dependencies,
                slots:factory.slots,
                dependenciesFrozen:Object.isFrozen(factory.dependencies),
                slotsFrozen:Object.isFrozen(factory.slots),
                rawTemplateWasNotParsed,
                legacyRendererError,
                navigationHref,
                resourceSrc,
                contextSensitiveSrc
            }));
            """;
    }

    private sealed record MetadataRuntimeResult(
        string[] Dependencies,
        string[] Slots,
        bool DependenciesFrozen,
        bool SlotsFrozen,
        bool RawTemplateWasNotParsed,
        string LegacyRendererError,
        string NavigationHref,
        string ResourceSrc,
        string ContextSensitiveSrc);
}
