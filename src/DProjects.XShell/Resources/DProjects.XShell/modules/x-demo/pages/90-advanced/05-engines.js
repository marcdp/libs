// contract
export const contract = {
    description: "State and render engine boundaries",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Engines</h2>

        <p>
            The Page/Component loader is the orchestrator. It owns the contract, public properties, controller, lifecycle, services, and coordination.
            The state engine owns reactive state; the render engine owns rendered output.
        </p>

        <pre x-pre><code>loader
    orchestration, contract, public API, controller, lifecycle, services
        │
        ├── state engine  → reactive state
        └── render engine → rendered output</code></pre>

        <h3>Effective x-demo engine selection</h3>

        <x-datafields>
            <x-datafield label="Page meta render engine"><code>{{ state.pageMetaRender }}</code></x-datafield>
            <x-datafield label="Page meta state engine"><code>{{ state.pageMetaState }}</code></x-datafield>
            <x-datafield label="Page effective render engine"><code>{{ state.pageRender }}</code></x-datafield>
            <x-datafield label="Page effective state engine"><code>{{ state.pageState }}</code></x-datafield>
            <x-datafield label="Component effective render engine"><code>{{ state.componentRender }}</code></x-datafield>
            <x-datafield label="Component effective state engine"><code>{{ state.componentState }}</code></x-datafield>
        </x-datafields>

        <pre x-pre><code>"defaults": {
    "page": {
        "renderEngine": "x",
        "stateEngine": "proxy"
    },
    "component": {
        "renderEngine": "x",
        "stateEngine": "proxy"
    }
}</code></pre>

        <p>
            The current loader selects definition <code>meta</code> first and falls back to the owning module's defaults. This Page has no engine
            override, so its effective values come from the x-demo module. XTemplate is one render engine; Pages and Components are not inherently
            XTemplate-based.
        </p>
    `,

    state: {
        pageMetaRender: "",
        pageMetaState: "",
        pageRender: "",
        pageState: "",
        componentRender: "",
        componentState: ""
    },

    controller({ state, definition, config }) {
        return {
            load() {
                // calculate the same meta-then-module fallback used by page-js
                const moduleDefaults = config.modules["x-demo"]?.defaults || {};
                const pageDefaults = moduleDefaults.page || {};
                const componentDefaults = moduleDefaults.component || {};
                const pageMeta = definition.meta || {};
                state.pageMetaRender = pageMeta.renderEngine || "(not set)";
                state.pageMetaState = pageMeta.stateEngine || "(not set)";
                state.pageRender = pageMeta.renderEngine || pageDefaults.renderEngine || "(missing)";
                state.pageState = pageMeta.stateEngine || pageDefaults.stateEngine || "(missing)";
                state.componentRender = componentDefaults.renderEngine || "(missing)";
                state.componentState = componentDefaults.stateEngine || "(missing)";
            }
        };
    }
};
