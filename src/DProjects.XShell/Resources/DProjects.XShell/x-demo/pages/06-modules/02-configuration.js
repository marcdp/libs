// contract
export const contract = {
    description: "Effective XShell configuration",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Configuration</h2>

        <p>
            Bootstrap builds one effective configuration before the browser runtime starts. It loads the framework's <code>xshell.jsonc</code>
            and the root module's <code>module.jsonc</code>, discovers referenced module definitions, normalizes them, and merges the fragments.
            XShell then receives that one object through <code>xshell.init(config)</code>; it does not query every module configuration separately.
        </p>

        <pre x-pre><code>xshell.jsonc
    +
root module.jsonc
    +
dependency module.jsonc files
        ↓
discovery + normalization + merge
        ↓
effective configuration
        ↓
xshell.init(config)</code></pre>

        <h3>The three main sections</h3>

        <ul>
            <li><code>config.app</code> — application-level bootstrap values such as <code>name</code>, <code>basePath</code>, and <code>params</code>.</li>
            <li><code>config.modules</code> — canonical module definitions keyed by module id, including normalized URLs and contributions.</li>
            <li><code>config.xshell</code> — framework/runtime configuration such as <code>environment</code>, <code>assetsPrefix</code>, navigation, Areas, and resolver settings.</li>
        </ul>

        <h3>Root module</h3>

        <p>
            The root module is the application's entry configuration. It is the single <code>modules</code> entry without <code>configUrl</code>,
            regardless of property order. Other entries reference dependencies. The root module is a module; <code>xshell.jsonc</code> is the
            separate framework configuration source.
        </p>

        <pre x-pre><code>{
    "modules": {
        "app": {
            "label": "Application",
            "version": "1.0.0",
            "defaults": {
                "page": { "renderEngine": "x", "stateEngine": "proxy" },
                "component": { "renderEngine": "x", "stateEngine": "proxy" }
            }
        },
        "x": {
            "configUrl": "source:../x/module.jsonc",
            "params": { "mode": "compact" }
        }
    }
}</code></pre>

        <h3>Discovery, normalization, and merge</h3>

        <p>
            Discovery finds the root and referenced configuration documents recursively. Normalization resolves <code>url:</code> references,
            records each module's <code>configUrl</code>, derives <code>assetsUrl</code> when needed, and maps module-relative resources into the
            runtime namespace <code>/_assets/&lt;module-id&gt;/...</code>. Merge combines the resulting fragments using the current Bootstrap
            registration and merge behavior.
        </p>

        <p>
            Plain objects merge recursively, arrays concatenate, and scalar values are replaced by the later value. The architecture documentation
            uses deterministic dependency-first precedence: framework defaults, dependencies, their dependents, and the root application last.
            Any reference may contribute dependency configuration; the root still has final authority without discovery-order races.
        </p>

        <h3>Read-only runtime view</h3>

        <p>
            The values below are selected from the effective configuration passed to the runtime. Bootstrap deeply freezes that object before
            calling <code>xshell.init(config)</code>, so runtime code should read it rather than mutate it.
        </p>

        <x-datafields>
            <x-datafield label="Application"><code>{{ state.app }}</code></x-datafield>
            <x-datafield label="Module ids"><code>{{ state.moduleIds }}</code></x-datafield>
            <x-datafield label="XShell runtime"><code>{{ state.xshell }}</code></x-datafield>
        </x-datafields>
    `,

    state: {
        app: "{}",
        moduleIds: "[]",
        xshell: "{}"
    },

    controller({ state, config }) {
        return {
            load() {
                // inspect a compact read-only view of the effective config
                state.app = JSON.stringify({
                    name: config.app?.name || "",
                    basePath: config.app?.basePath || "",
                    params: config.app?.params || {}
                });
                state.moduleIds = JSON.stringify(Object.keys(config.modules || {}));
                state.xshell = JSON.stringify({
                    environment: config.xshell?.environment || "",
                    assetsPrefix: config.xshell?.assetsPrefix || "",
                    navigation: config.xshell?.navigation || {}
                });
            }
        };
    }
};
