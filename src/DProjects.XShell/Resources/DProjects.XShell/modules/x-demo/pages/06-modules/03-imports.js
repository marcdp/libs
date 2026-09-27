// contract
export const contract = {
    description: "Module imports",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Imports</h2>

        <p>
            An import points to another module definition. Bootstrap follows imports recursively, resolves each definition URL, and registers a
            definition URL only once.
        </p>

        <pre x-pre><code>"imports": [
    {
        "configUrl": "url:../x/module.jsonc",
        "params": {
            "mode": "normal"
        }
    }
]</code></pre>

        <pre x-pre><code>module imports
    ↓
bootstrap discovers definitions recursively
    ↓
definition URLs are deduplicated
    ↓
one canonical module definition/runtime instance</code></pre>

        <p>
            Repeating the same resolved definition URL does not create another module instance. The first registered import keeps its parameters;
            later repeated imports do not override or merge them. Discovery and registration order therefore matters.
        </p>

        <h3>Current x-demo imports</h3>

        <ul>
            <li x-for="item in state.imports" x-key="id">
                <code>{{ item.configUrl }}</code> with params <code>{{ item.params }}</code>
            </li>
            <li x-if="!state.imports.length">This module has no direct imports.</li>
        </ul>

        <h3>Effective module ids</h3>

        <p>
            The list below is a read-only view of <code>config.modules</code>. A single entry per id is the visible result of canonical definition
            registration.
        </p>

        <ul>
            <li x-for="id in state.moduleIds" x-key="id"><code>{{ id }}</code></li>
        </ul>
    `,

    state: {
        imports: [],
        moduleIds: []
    },

    controller({ state, config }) {
        return {
            load() {
                // inspect imports and the effective module map
                const moduleConfig = config.modules["x-demo"] || {};
                state.imports = (moduleConfig.imports || []).map((item, index) => ({
                    id: String(index),
                    configUrl: item.configUrl || "",
                    params: JSON.stringify(item.params || {})
                }));
                state.moduleIds = Object.keys(config.modules);
            }
        };
    }
};
