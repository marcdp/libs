// contract
export const contract = {
    description: "Module resources",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Resources</h2>

        <p>
            Module resources use a stable logical namespace, regardless of where the module is physically stored:
            <code>/_assets/&lt;module-id&gt;/&lt;resource&gt;</code>.
        </p>

        <ul>
            <li><code>/_assets/x/components/x-button.js</code></li>
            <li><code>/_assets/x-demo/pages/06-modules/index.js</code></li>
        </ul>

        <pre x-pre><code>client URL
    ↓
Service Worker mapping
    ↓
module assetsUrl
    ↓
physical resource</code></pre>

        <p>
            The Service Worker maps the client-facing prefix to the module's physical <code>assetsUrl</code>. Application code should depend on the
            stable <code>/_assets/&lt;module-id&gt;/...</code> URL, not on the storage layout.
        </p>

        <p>
            Expanded module directories are currently supported. ZIP-backed module resource loading is planned but not implemented.
        </p>

        <h3>Current x-demo resource mapping</h3>

        <x-datafields>
            <x-datafield label="configUrl"><code>{{ state.configUrl }}</code></x-datafield>
            <x-datafield label="assetsUrl"><code>{{ state.assetsUrl }}</code></x-datafield>
            <x-datafield label="logical asset prefix"><code>{{ state.logicalPrefix }}</code></x-datafield>
        </x-datafields>
    `,

    state: {
        configUrl: "",
        assetsUrl: "",
        logicalPrefix: "/_assets/x-demo/"
    },

    controller({ state, config }) {
        return {
            load() {
                // inspect the physical mapping recorded by bootstrap
                const moduleConfig = config.modules["x-demo"] || {};
                state.configUrl = moduleConfig.configUrl || "";
                state.assetsUrl = moduleConfig.assetsUrl || "";
            }
        };
    }
};
