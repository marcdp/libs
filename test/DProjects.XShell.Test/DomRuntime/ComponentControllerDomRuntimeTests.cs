using System.Diagnostics;
using System.Text.Json;

namespace DProjects.XShell.Test;

public sealed class ComponentControllerDomRuntimeTests {

    // methods
    [Fact]
    public void ControllerInvocationsUseControllerContextAndExplicitHost() {
        var result = ExecuteRuntimeScenario();

        Assert.True(result.HostInjected);
        Assert.True(result.LoadContext);
        Assert.True(result.MountContext);
        Assert.True(result.UnmountContext);
        Assert.True(result.UnloadContext);
        Assert.True(result.StateChangeContext);
        Assert.True(result.TemplateContext);
        Assert.True(result.PublicContext);
        Assert.True(result.InternalContext);
        Assert.True(result.EventsContext);
        Assert.True(result.TimerContext);
        Assert.True(result.PublicArgumentsPreserved);
        Assert.True(result.PrivateMethodHidden);
        Assert.True(result.CommandDispatcherHidden);
        Assert.Equal(1, result.LoadCount);
        Assert.Equal(2, result.MountCount);
        Assert.Equal(2, result.UnmountCount);
        Assert.Equal(1, result.UnloadCount);
    }

    [Fact]
    public void PublicContractMethodsRequireControllerImplementationsAndCannotReplaceRuntimeMethods() {
        var result = ExecuteRuntimeScenario();

        Assert.Contains("controller.missing is not a function", result.MissingMethodError);
        Assert.Contains("cannot expose public method 'unload'", result.RuntimeCollisionError);
    }

    [Fact]
    public void SlotValidationUsesRenderEngineFactoryMetadataAndAllowsContractSupersets() {
        var result = ExecuteRuntimeScenario();

        Assert.True(result.RawTemplateIgnored);
        Assert.True(result.ContractSupersetAccepted);
        Assert.Contains("slot 'footer'", result.MissingNamedSlotError);
        Assert.Contains("slot '(default)'", result.MissingDefaultSlotError);
    }

    // methods (private)
    private static ControllerRuntimeResult ExecuteRuntimeScenario() {
        var runtimePath = Path.Combine(AppContext.BaseDirectory, "Resources", "DProjects.XShell", "xshell", "loaders", "component-js.js");
        Assert.True(File.Exists(runtimePath), $"The copied component runtime was not found at '{runtimePath}'.");
        var modulePath = Path.Combine(Path.GetTempPath(), $"component-controller-runtime-{Guid.NewGuid():N}.mjs");
        try {
            File.WriteAllText(modulePath, CreateJavaScriptModule(runtimePath));
            using var process = Process.Start(new ProcessStartInfo("node", modulePath) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"Component controller runtime failed:{Environment.NewLine}{error}");
            return JsonSerializer.Deserialize<ControllerRuntimeResult>(output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Component controller runtime returned no result.");
        } finally {
            if (File.Exists(modulePath)) File.Delete(modulePath);
        }
    }

    private static string CreateJavaScriptModule(string runtimePath) {
        return $$"""
            class FakeEventTarget {
                listeners = new Map();
                addEventListener(name, listener) {
                    const listeners = this.listeners.get(name) ?? [];
                    listeners.push(listener);
                    this.listeners.set(name, listeners);
                }
                removeEventListener(name, listener) {
                    this.listeners.set(name, (this.listeners.get(name) ?? []).filter(item => item !== listener));
                }
                dispatchEvent(event) {
                    for (const listener of this.listeners.get(event.type) ?? []) listener.call(this, event);
                }
            }
            class FakeShadowRoot extends FakeEventTarget {
                adoptedStyleSheets = [];
                querySelector() { return null; }
                querySelectorAll() { return []; }
            }
            class FakeElement extends FakeEventTarget {
                attributes = [];
                parentNode = null;
                isConnected = true;
                constructor() {
                    super();
                    this.shadowRoot = null;
                }
                attachShadow() {
                    this.shadowRoot = new FakeShadowRoot();
                    this.shadowRoot.host = this;
                    return this.shadowRoot;
                }
                getAttribute(name) { return this.attributes.find(attribute => attribute.name === name)?.value ?? null; }
                setAttribute(name, value) {
                    const attribute = this.attributes.find(item => item.name === name);
                    if (attribute) attribute.value = String(value);
                    else this.attributes.push({ name, value: String(value) });
                }
                removeAttribute(name) { this.attributes = this.attributes.filter(attribute => attribute.name !== name); }
                getRootNode() { return this; }
            }
            class FakeTemplate extends FakeElement {
                content = { querySelectorAll() { return []; } };
                set innerHTML(value) {}
            }
            const registry = new Map();
            globalThis.HTMLElement = FakeElement;
            globalThis.CSSStyleSheet = class { replaceSync() {} };
            globalThis.MutationObserver = class { observe() {} disconnect() {} };
            globalThis.CustomEvent = class { constructor(type, options = {}) { this.type = type; Object.assign(this, options); } };
            globalThis.customElements = { get(name) { return registry.get(name); }, define(name, type) { registry.set(name, type); } };
            globalThis.window = { customElements: globalThis.customElements };
            globalThis.document = {
                baseURI: "http://localhost/",
                body: new FakeElement(),
                adoptedStyleSheets: [],
                createElement(tag) { return tag.toLowerCase() === "template" ? new FakeTemplate() : new FakeElement(); }
            };
            globalThis.requestAnimationFrame = callback => { callback(); return 1; };

            const [{ createComponentClassFromJsDefinition }, { default: xshell }] = await Promise.all([
                import({{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}),
                import(new URL("../xshell.js", {{JsonSerializer.Serialize(new Uri(runtimePath).AbsoluteUri)}}))
            ]);

            class StateEngineFactory {
                constructor(state) { this.state = state; }
                create() { return { ...this.state }; }
            }
            let templateHandler;
            class RenderEngineFactory {
                constructor(template, context, templateRenderer) {
                    this.dependencies = [];
                    this.slots = templateRenderer?.slots ?? [];
                }
                init() {}
                create(options) {
                    templateHandler = options.handler;
                    return { mount() {}, unmount() {}, render() {} };
                }
            }
            xshell._config = { modules: { test: { defaults: { component: { stateEngine: "test", renderEngine: "test" } } } } };
            xshell._loader = { async load(resource) { return resource.startsWith("state-engine:") ? StateEngineFactory : RenderEngineFactory; } };
            xshell._services = { resolve() { return null; } };
            xshell._modules = { getModuleById() { return {}; } };

            const contexts = [];
            const eventTarget = new FakeEventTarget();
            let controller;
            let injectedHost;
            const definition = {
                meta: { name: "x-controller-runtime-test" },
                state: {},
                style: "",
                template: "",
                controller({ host, events, timer }) {
                    injectedHost = host;
                    controller = {
                        load() {
                            contexts.push(["load", this === controller]);
                            events.on(eventTarget, "run", "eventsHandler");
                            timer.setTimeout(0, "timerHandler");
                        },
                        mount() { contexts.push(["mount", this === controller]); this.refresh(); },
                        unmount() { contexts.push(["unmount", this === controller]); },
                        unload() { contexts.push(["unload", this === controller]); },
                        stateChange() { contexts.push(["stateChange", this === controller]); },
                        refresh() { contexts.push(["refresh", this === controller]); },
                        templateHandler() { contexts.push(["template", this === controller]); },
                        eventsHandler() { contexts.push(["events", this === controller]); },
                        timerHandler() { contexts.push(["timer", this === controller]); },
                        publicMethod(...args) {
                            contexts.push(["public", this === controller]);
                            return args;
                        }
                    };
                    return controller;
                }
            };
            const contract = {
                description: "Controller runtime test.",
                properties: {},
                events: {},
                slots: {},
                methods: { publicMethod: { description: "Public test method." } }
            };
            const context = { resourceDefinition: { moduleId: "test" } };
            const Component = await createComponentClassFromJsDefinition("runtime-test", context, definition, contract);
            const element = new Component();
            element.connectedCallback();
            templateHandler("templateHandler", { event: { type: "click" } });
            const publicArguments = element.publicMethod({ value: 1 }, "two");
            eventTarget.dispatchEvent({ type: "run" });
            await new Promise(resolve => setTimeout(resolve, 10));
            element.disconnectedCallback();
            element.connectedCallback();
            element.disconnectedCallback();
            await element.unload();

            let missingMethodError = "";
            try {
                const Missing = await createComponentClassFromJsDefinition("missing-test", context, {
                    meta: { name: "x-controller-missing-test" }, state: {}, style: "", template: "", controller() { return {}; }
                }, { description: "Missing method test.", properties: {}, events: {}, slots: {}, methods: { missing: { description: "Missing." } } });
                new Missing();
            } catch (error) {
                missingMethodError = error.message;
            }

            let runtimeCollisionError = "";
            try {
                await createComponentClassFromJsDefinition("collision-test", context, {
                    meta: { name: "x-controller-collision-test" }, state: {}, style: "", template: "", controller() { return { unload() {} }; }
                }, { description: "Collision test.", properties: {}, events: {}, slots: {}, methods: { unload: { description: "Collision." } } });
            } catch (error) {
                runtimeCollisionError = error.message;
            }

            let rawTemplateIgnored = false;
            try {
                await createComponentClassFromJsDefinition("raw-slot-test", context, {
                    meta: { name: "x-raw-slot-test" }, state: {}, style: "", template: '<slot name="ghost"></slot>',
                    templateRenderer: { slots: [] }, controller() { return {}; }
                }, { description: "Raw slot test.", properties: {}, events: {}, slots: {}, methods: {} });
                rawTemplateIgnored = true;
            } catch {}

            let contractSupersetAccepted = false;
            try {
                await createComponentClassFromJsDefinition("slot-superset-test", context, {
                    meta: { name: "x-slot-superset-test" }, state: {}, style: "", template: "",
                    templateRenderer: { slots: ["footer"] }, controller() { return {}; }
                }, { description: "Slot superset test.", properties: {}, events: {}, slots: { footer: { description: "Footer." }, tools: { description: "Tools." } }, methods: {} });
                contractSupersetAccepted = true;
            } catch {}

            let missingNamedSlotError = "";
            try {
                await createComponentClassFromJsDefinition("missing-named-slot-test", context, {
                    meta: { name: "x-missing-named-slot-test" }, state: {}, style: "", template: "",
                    templateRenderer: { slots: ["footer"] }, controller() { return {}; }
                }, { description: "Missing named slot test.", properties: {}, events: {}, slots: {}, methods: {} });
            } catch (error) {
                missingNamedSlotError = error.message;
            }

            let missingDefaultSlotError = "";
            try {
                await createComponentClassFromJsDefinition("missing-default-slot-test", context, {
                    meta: { name: "x-missing-default-slot-test" }, state: {}, style: "", template: "",
                    templateRenderer: { slots: [""] }, controller() { return {}; }
                }, { description: "Missing default slot test.", properties: {}, events: {}, slots: {}, methods: {} });
            } catch (error) {
                missingDefaultSlotError = error.message;
            }

            const count = name => contexts.filter(([contextName]) => contextName === name).length;
            const hasValidContext = name => contexts.filter(([contextName]) => contextName === name).every(([, valid]) => valid) && count(name) > 0;
            console.log(JSON.stringify({
                hostInjected: injectedHost === element,
                loadContext: hasValidContext("load"),
                mountContext: hasValidContext("mount"),
                unmountContext: hasValidContext("unmount"),
                unloadContext: hasValidContext("unload"),
                stateChangeContext: hasValidContext("stateChange"),
                templateContext: hasValidContext("template"),
                publicContext: hasValidContext("public"),
                internalContext: hasValidContext("refresh"),
                eventsContext: hasValidContext("events"),
                timerContext: hasValidContext("timer"),
                publicArgumentsPreserved: publicArguments[0].value === 1 && publicArguments[1] === "two",
                privateMethodHidden: element.templateHandler === undefined && element.refresh === undefined,
                commandDispatcherHidden: element.onCommand === undefined,
                loadCount: count("load"),
                mountCount: count("mount"),
                unmountCount: count("unmount"),
                unloadCount: count("unload"),
                missingMethodError,
                runtimeCollisionError,
                rawTemplateIgnored,
                contractSupersetAccepted,
                missingNamedSlotError,
                missingDefaultSlotError
            }));
            """;
    }

    private sealed record ControllerRuntimeResult(
        bool HostInjected,
        bool LoadContext,
        bool MountContext,
        bool UnmountContext,
        bool UnloadContext,
        bool StateChangeContext,
        bool TemplateContext,
        bool PublicContext,
        bool InternalContext,
        bool EventsContext,
        bool TimerContext,
        bool PublicArgumentsPreserved,
        bool PrivateMethodHidden,
        bool CommandDispatcherHidden,
        int LoadCount,
        int MountCount,
        int UnmountCount,
        int UnloadCount,
        string MissingMethodError,
        string RuntimeCollisionError,
        bool RawTemplateIgnored,
        bool ContractSupersetAccepted,
        string MissingNamedSlotError,
        string MissingDefaultSlotError);
}
