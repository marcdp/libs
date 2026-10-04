// contract
export const contract = {
    description: "Runtime Resolver and Loader resources",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Runtime loading</h2>

        <p>
            Declarative <code>definition.dependencies</code> are known when a Page or Component class is created. The injected <code>loader</code>
            service is for resources selected dynamically while the Page is running.
        </p>

        <pre x-pre><code>logical reference
    ↓
Resolver
    ↓
resolved URL + loader metadata
    ↓
Loader
    ↓
loaded runtime value</code></pre>

        <p>
            <label>
                Resource
                <select x-model="state.resource">
                    <option value="component:x-button">component:x-button</option>
                    <option value="icon:x-file">icon:x-file</option>
                    <option value="page:/_assets/x-demo/pages/06-modules/index.js">page:/_assets/x-demo/pages/06-modules/index.js</option>
                </select>
            </label>
            <button x-on:click="loadResource">Resolve and load</button>
        </p>

        <x-datafields>
            <x-datafield label="Logical reference"><code>{{ state.result.resource }}</code></x-datafield>
            <x-datafield label="Resolved URL"><code>{{ state.result.url }}</code></x-datafield>
            <x-datafield label="Resource loader"><code>{{ state.result.loader }}</code></x-datafield>
            <x-datafield label="Loaded value"><span>{{ state.result.value }}</span></x-datafield>
        </x-datafields>

        <p x-if="state.error" class="error">{{ state.error }}</p>

        <h3>Loader cache identity</h3>

        <p>
            Resolver definitions can opt into <code>cacheMode: "full"</code> or <code>cacheMode: "path"</code>. Full mode includes the query in
            the cache key; path mode excludes it. Generated Page rules use path identity because the Page implementation is query-independent while
            each Page instance retains its complete <code>src</code> and query.
        </p>
    `,

    state: {
        resource: "component:x-button",
        error: "",
        result: {
            resource: "(choose a resource)",
            url: "",
            loader: "",
            value: ""
        }
    },

    controller({ state, loader, resolver }) {
        return {
            async loadResource() {
                // resolve metadata first, then use the injected Loader service
                state.error = "";
                const resource = state.resource;
                const resolved = resolver.resolve(resource);
                if (!resolved) {
                    state.error = `Resolver could not resolve '${resource}'.`;
                    return;
                }
                try {
                    const value = await loader.load(resource);
                    let valueDescription = typeof value;
                    if (typeof value === "function") {
                        valueDescription = `${valueDescription} ${value.name || "(anonymous function)"}`;
                    } else if (value?.nodeType) {
                        valueDescription = `${value.constructor.name} <${value.localName}>`;
                    } else if (value?.constructor?.name) {
                        valueDescription = value.constructor.name;
                    }
                    state.result = {
                        resource,
                        url: resolved.url,
                        loader: resolved.definition.loader,
                        value: valueDescription
                    };
                } catch (error) {
                    state.error = error.message || String(error);
                }
            }
        };
    }
};
