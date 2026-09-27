// contract
export const contract = {
    description: "Module controllers",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Controller</h2>

        <p>
            A module controller is the executable part of a module definition. XShell constructs it with registered services plus the module's
            <code>params</code> and calls its lifecycle methods.
        </p>

        <pre x-pre><code>export default class {
    constructor({ navigation, bus, config, params }) {
        this.navigation = navigation;
        this.bus = bus;
        this.config = config;
        this.params = params;
    }

    async start() {
        // module startup
    }

    async stop() {
        // module shutdown when Modules.stop() is called
    }
}</code></pre>

        <pre x-pre><code>Modules.init()
    loads module styles/controllers
    ↓
awaits resources
    ↓
calls controller.start()</code></pre>

        <p>
            Module startup completes before Areas initialization. A controller can therefore register dynamic menu sources before Areas composes
            menus. The current lifecycle is <code>start()</code> and <code>stop()</code>; legacy <code>onCommand("load")</code> is not the module
            lifecycle. Application shutdown does not currently invoke <code>stop()</code> automatically.
        </p>

        <h3>Current x-demo runtime record</h3>

        <x-datafields>
            <x-datafield label="Module id"><code>{{ state.id }}</code></x-datafield>
            <x-datafield label="Controller type"><code>{{ state.controllerType }}</code></x-datafield>
            <x-datafield label="Params"><code>{{ state.params }}</code></x-datafield>
        </x-datafields>
    `,

    state: {
        id: "",
        controllerType: "",
        params: "{}"
    },

    controller({ state, modules }) {
        return {
            load() {
                // inspect the live controller without invoking it
                const module = modules.getModuleById("x-demo");
                state.id = module?.id || "";
                state.controllerType = module?.controller?.constructor?.name || "";
                state.params = JSON.stringify(module?.params || {});
            }
        };
    }
};
