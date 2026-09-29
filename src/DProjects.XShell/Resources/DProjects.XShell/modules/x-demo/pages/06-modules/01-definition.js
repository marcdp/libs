// contract
export const contract = {
    description: "Module definitions",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Definition</h2>

        <p>
            A module definition is the canonical declarative entry at <code>config.modules.&lt;module-id&gt;</code>. It describes the metadata and
            contributions that belong to one module: styles, a controller, menus, defaults, and optional contract declarations. The object key is
            the authoritative module id; the definition does not repeat it in a <code>name</code> or <code>moduleId</code> field.
        </p>

        <h3>Definition fragment</h3>

        <pre x-pre><code>{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "copyright": "",
            "icon": "",
            "controller": "./js/module.js",
            "styles": [],
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            },
            "menus": {}
        }
    }
}</code></pre>

        <p>
            Bootstrap adds the normalized <code>configUrl</code> and <code>assetsUrl</code> values to every effective module definition. They have
            different roles:
        </p>

        <ul>
            <li><code>configUrl</code> identifies the location of the module's <code>module.jsonc</code>.</li>
            <li><code>assetsUrl</code> identifies the physical directory or package that contains module resources.</li>
        </ul>

        <p>
            Runtime clients normally use <code>/_assets/&lt;module-id&gt;/...</code>. They should not couple themselves to the physical
            <code>assetsUrl</code> layout.
        </p>

        <h3>Current x-demo definition</h3>

        <x-datafields>
            <x-datafield label="Module id"><code>{{ state.current.id }}</code></x-datafield>
            <x-datafield label="Label"><span>{{ state.current.label }}</span></x-datafield>
            <x-datafield label="Version"><span>{{ state.current.version }}</span></x-datafield>
            <x-datafield label="configUrl"><code>{{ state.current.configUrl }}</code></x-datafield>
            <x-datafield label="assetsUrl"><code>{{ state.current.assetsUrl }}</code></x-datafield>
        </x-datafields>

        <p>These values are read from the frozen effective configuration; this page does not modify it.</p>
    `,

    state: {
        current: {
            id: "",
            label: "",
            version: "",
            configUrl: "",
            assetsUrl: ""
        }
    },

    controller({ state, config }) {
        return {
            load() {
                // inspect one canonical definition
                const moduleConfig = config.modules["x-demo"] || {};
                state.current = {
                    id: "x-demo",
                    label: moduleConfig.label || "",
                    version: moduleConfig.version || "",
                    configUrl: moduleConfig.configUrl || "",
                    assetsUrl: moduleConfig.assetsUrl || ""
                };
            }
        };
    }
};
