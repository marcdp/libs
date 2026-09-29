// contract
export const contract = {
    description: "Module dependencies and root composition",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Module dependencies</h2>

        <p>
            Every <code>module.jsonc</code> owns exactly one local definition: the <code>modules.&lt;id&gt;</code> entry without
            <code>configUrl</code>. Every other entry is a dependency reference whose key is the expected module id.
        </p>

        <pre x-pre><code>"modules": {
    "x-demo": {
        "label": "test module",
        "version": "1.0.0"
    },
    "x": {
        "configUrl": "url:../x/module.jsonc",
        "params": {
            "aaa": 890,
            "bbb": 987
        }
    }
}</code></pre>

        <pre x-pre><code>module references
    ↓
bootstrap discovers definitions recursively
    ↓
definition URLs are deduplicated
    ↓
one canonical module definition/runtime instance</code></pre>

        <p>
            Repeating the same id and resolved URL fetches the definition once and does not create another runtime instance. A mismatched local id,
            conflicting URLs for one id, or a dependency cycle is rejected. Child modules may declare dependencies but cannot supply their
            <code>params</code>; only the root application composes parameter values and overrides.
        </p>

        <h3>Current x-demo dependency references</h3>

        <ul>
            <li x-for="item in state.references" x-key="id">
                <strong>{{ item.id }}</strong> → <code>{{ item.configUrl }}</code> with params <code>{{ item.params }}</code>
            </li>
            <li x-if="!state.references.length">This module has no direct dependency references.</li>
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
        references: [],
        moduleIds: []
    },

    controller({ state, config }) {
        return {
            load() {
                // inspect the effective references composed by the root application
                state.references = Object.entries(config.modules)
                    .filter(([id]) => id !== "x-demo")
                    .map(([id, item]) => ({ id, configUrl: item.configUrl || "", params: JSON.stringify(item.params || {}) }));
                state.moduleIds = Object.keys(config.modules);
            }
        };
    }
};
