using DProjects.XShell.Services;

var tests = new CompilerTests();
await tests.RunAsync();

internal sealed class CompilerTests {

    // vars
    private int mAssertions;

    // methods
    public async Task RunAsync() {
        TestTemplateDirectives();
        TestTemplateErrors();
        await TestSourceTransformationAsync();
        Console.WriteLine($"Passed {mAssertions} X Template compiler assertions.");
    }

    // methods (private)
    private void TestTemplateDirectives() {
        var compiler = new XTemplateCompiler();
        AssertContains(compiler.Compile("{{ state.name }}"), "\"\" + ( state.name )");
        AssertContains(compiler.Compile("{{ state.value < 3 }}"), "state.value < 3");
        AssertContains(compiler.Compile("<span x-text=\"state.name\"></span>"), "\"\" + state.name");
        AssertContains(compiler.Compile("<x-page src=\"/pages/test.js\"></x-page>"), "utils.rewriteAttribute");
        AssertContains(compiler.Compile("<span x-html=\"state.html\"></span>"), "format:\"html\"");
        AssertContains(compiler.Compile("<span x-children=\"state.nodes\"></span>"), "format:\"node\"");
        AssertContains(compiler.Compile("<div x-attr=\"state.attrs\" :title=\"state.title\" :[state.name]=\"state.value\"></div>"), "utils.toDynamicArgument(state.name, state.value)");
        AssertContains(compiler.Compile("<input x-prop=\"state.props\" .value=\"state.value\" .[state.name]=\"state.value\">"), "utils.toDynamicProperty(state.name, state.value)");
        AssertContains(compiler.Compile("<input x-prop=\"state.props\">"), "{...state.props}");
        AssertContains(compiler.Compile("<button x-on:keydown.enter=\"save\"></button>"), "handler(\"save\", event)");
        AssertContains(compiler.Compile("<div x-if=\"state.a\"></div><div x-elseif=\"state.b\"></div><div x-else></div>"), "'x-elseif'");
        AssertContains(compiler.Compile("<li x-for=\"item in state.items\">{{item.name}}</li>"), "'x-for-start'");
        AssertContains(compiler.Compile("<li x-for=\"(item,index) in state.items\" x-key=\"id\"></li>"), "\"key\":item.id");
        AssertContains(compiler.Compile("<li x-recursive=\"item in state.items\" x-recursive-wrapper=\"ul\">{{item.name}}</li>"), "func(item.children");
        AssertContains(compiler.Compile("<div x-show=\"state.visible\"></div>"), "style:'display:none'");
        AssertContains(compiler.Compile("<div class=\"base\" x-class:selected=\"state.selected\"></div>"), "state.selected ? \"selected\"");
        AssertContains(compiler.Compile("<input x-model=\"state.name\">"), "state.name = value; invalidate()");
        AssertContains(compiler.Compile("<div x-once></div>"), "renderCount==0");
        AssertContains(compiler.Compile("<pre x-pre>{{ state.raw }}</pre>"), "{{ state.raw }}");
    }

    private void TestTemplateErrors() {
        var compiler = new XTemplateCompiler();
        AssertThrows(() => compiler.Compile("<div x-unknown=\"state.value\"></div>"), "invalid X template directive");
        AssertThrows(() => compiler.Compile("<li x-for=\"item of state.items\"></li>"), "Invalid x-for expression");
        AssertThrows(() => compiler.Compile("<div><span></div>"), "unexpected closing tag");
        AssertThrows(() => compiler.Compile("<div x-if=\"state.a\" x-for=\"item in state.items\"></div>"), "more than one structural directive");
    }

    private async Task TestSourceTransformationAsync() {
        var source = """
            // template: `comment decoy`
            export const contract = {};
            const decoy = "template: `string decoy`";
            const nestedTemplate = `outer ${state.flag ? `inner` : `other`}`;
            const regexp = () => /template:\s*`[^`]+`/;
            export default {
                nested: { template: `nested decoy` },
                template: `
                    <div>{{ state.name }}</div>
                `,
                script() { return { text: "template: `method decoy`" }; }
            };
            """;
        var transformed = await TransformAsync(source);
        AssertContains(transformed, "export const contract = {};");
        AssertContains(transformed, "nested: { template: `nested decoy` }");
        AssertContains(transformed, "templateHandler: (state, handler, invalidate, utils, i18n, renderCount)");
        AssertEqual(transformed, await TransformAsync(transformed), "Compilation must be idempotent.");
        await AssertThrowsAsync(() => TransformAsync("export default { template: getTemplate() };"), "static template literal");
        await AssertThrowsAsync(() => TransformAsync("export default { template: `${state.name}` };"), "must be static");
    }

    private async Task<string> TransformAsync(string source) {
        var directory = Path.Combine(Path.GetTempPath(), "DProjects.XShell.CompilerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            var modulePath = Path.Combine(directory, "module.jsonc");
            var sourcePath = Path.Combine(directory, "component.js");
            await File.WriteAllTextAsync(modulePath, "{ \"modules\": { \"test\": { \"defaults\": { \"page\": { \"renderEngine\": \"x\" }, \"component\": { \"renderEngine\": \"x\" } } } } }");
            await File.WriteAllTextAsync(sourcePath, source);
            return await new ModuleFileCompiler().CompileToJsAsync(modulePath, sourcePath);
        } finally {
            Directory.Delete(directory, true);
        }
    }

    private void AssertContains(string actual, string expected) {
        mAssertions++;
        if (!actual.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Expected generated output to contain: {expected}\nActual:\n{actual}");
    }
    private void AssertEqual(string expected, string actual, string message) {
        mAssertions++;
        if (expected != actual) throw new InvalidOperationException(message);
    }
    private void AssertThrows(Action action, string expectedMessage) {
        mAssertions++;
        try { action(); } catch (Exception exception) when (exception.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)) { return; }
        throw new InvalidOperationException($"Expected an exception containing '{expectedMessage}'.");
    }
    private async Task AssertThrowsAsync(Func<Task> action, string expectedMessage) {
        mAssertions++;
        try { await action(); } catch (Exception exception) when (exception.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)) { return; }
        throw new InvalidOperationException($"Expected an exception containing '{expectedMessage}'.");
    }
}
