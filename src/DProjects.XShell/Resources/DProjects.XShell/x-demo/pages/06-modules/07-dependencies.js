// contract
export const contract = {
    description: "Consuming module resources",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Dependencies</h2>

        <p>
            Components and Pages consume module resources through logical references. The normal Resolver and Loader path turns a logical name into
            a URL, loader metadata, and finally a resource-specific runtime value.
        </p>

        <pre x-pre><code>logical resource
    ↓
Resolver
    ↓
resource location + loader metadata
    ↓
Loader
    ↓
resource-specific loader</code></pre>

        <ul>
            <li><code>component:x-button</code></li>
            <li><code>icon:x-file</code></li>
            <li><code>module:/_assets/x/utils/...</code></li>
            <li><code>page:/_assets/x-demo/pages/...</code></li>
        </ul>

        <h3>Declarative dependencies</h3>

        <pre x-pre><code>export default {
    dependencies: {
        button: "component:x-button",
        fileIcon: "icon:x-file"
    },

    controller({ dependencies }) {
        return {
            load() {
                // dependencies.button
                // dependencies.fileIcon
            }
        };
    }
};</code></pre>

        <p>
            The Loader resolves and loads the declared values before controller creation, then exposes them as
            <code>controller({ dependencies })</code>. This uses the normal Resolver + Loader path; it is not a second dependency-injection
            system. Controllers can still use the injected <code>loader</code> service for dynamic or runtime-dependent loads.
        </p>

        <h3>Live resolver results</h3>

        <ul>
            <li x-for="item in state.resources" x-key="resource">
                <code>{{ item.resource }}</code> → <code>{{ item.url }}</code> using <code>{{ item.loader }}</code>
            </li>
        </ul>
    `,

    state: {
        resources: []
    },

    controller({ state, resolver }) {
        return {
            load() {
                // inspect resolver metadata without loading or mutating resources
                const resources = [
                    "component:x-button",
                    "icon:x-file",
                    "page:/_assets/x-demo/pages/06-modules/index.js"
                ];
                state.resources = resources.map(resource => {
                    const resolved = resolver.resolve(resource);
                    return {
                        resource,
                        url: resolved?.url || "(not resolved)",
                        loader: resolved?.definition?.loader || "(none)"
                    };
                });
            }
        };
    }
};
