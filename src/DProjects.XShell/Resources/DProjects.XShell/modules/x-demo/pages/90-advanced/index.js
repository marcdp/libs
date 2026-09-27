// contract
export const contract = {
    description: "Cross-cutting XShell runtime mechanisms",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Advanced runtime</h2>

        <p>
            These examples move from individual features to the runtime mechanisms that connect Pages, Components, and Modules: lifecycle ownership,
            controller services, Bus communication, Area composition, engine boundaries, and dynamic resource loading.
        </p>

        <pre x-pre><code>Page / Component / Module
        │
        ├── lifecycle
        ├── injected services
        ├── Bus communication
        ├── Area context
        ├── state/render engines
        └── Resolver + Loader</code></pre>

        <p>
            Use the navigation entries for a short explanation, a focused source example, and a live view of the current XShell runtime where the
            public API makes that useful.
        </p>
    `
};
