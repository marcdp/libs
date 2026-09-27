// contract
export const contract = {
    description: "Module model overview",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Modules</h2>

        <p>
            A Module is the unit that packages configuration, resources, optional styles, menu contributions, and a controller. XShell turns the
            module graph into one canonical definition and one live runtime record per module id.
        </p>

        <h3>The module mental model</h3>

        <pre x-pre><code>root module
    ↓ imports
module definitions
    ↓ bootstrap
canonical effective config
    ↓ runtime
one module instance per module id</code></pre>

        <p>
            The root module is the application entry point. Its imports are discovered recursively, definitions are deduplicated by resolved
            configuration URL, and the effective <code>config.modules</code> object is used to create runtime module records.
        </p>

        <h3>Resources have a stable client namespace</h3>

        <pre x-pre><code>module resource
    ↓
/_assets/&lt;module-id&gt;/...
    ↓
Service Worker
    ↓
assetsUrl</code></pre>

        <p>
            The Service Worker hides the physical asset container. Components, Pages, icons, styles, and module controllers can therefore use the
            stable <code>/_assets/&lt;module-id&gt;/...</code> namespace even when the module's storage layout changes.
        </p>

        <h3>Six topics</h3>

        <ul>
            <li><strong>Definition</strong> — the canonical metadata and contributions for a module.</li>
            <li><strong>Imports</strong> — recursive discovery, URL deduplication, and first-registration parameters.</li>
            <li><strong>Resources</strong> — virtual asset URLs and Service Worker mapping.</li>
            <li><strong>Controller</strong> — startup and shutdown hooks for a live module instance.</li>
            <li><strong>Menus</strong> — static and dynamic contributions composed by Areas.</li>
            <li><strong>Dependencies</strong> — declarative resource consumption through Resolver and Loader.</li>
        </ul>

        <h3>Current effective modules</h3>

        <ul>
            <li x-for="module in state.modules" x-key="id">
                <strong>{{ module.id }}</strong> — {{ module.label }}; runtime path <code>{{ module.path }}</code>
            </li>
        </ul>
    `,

    state: {
        modules: []
    },

    controller({ state, modules }) {
        return {
            load() {
                // inspect the canonical runtime module records
                state.modules = modules.getModules().map(module => ({
                    id: module.id,
                    label: module.label,
                    path: module.path
                }));
            }
        };
    }
};
