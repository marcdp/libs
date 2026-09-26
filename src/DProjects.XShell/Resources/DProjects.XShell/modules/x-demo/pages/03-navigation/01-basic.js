// contract
export const contract = {
    description: "Basic Page navigation",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Basic navigation</h2>

        <p>
            Use <code>x-anchor</code> for declarative navigation. XShell handles an ordinary primary click and asks Navigation to open the
            destination; the generated link still behaves like a normal link for modifier keys and other browser features.
        </p>

        <p>
            <x-anchor href="/demo/pages" open="auto">Open the Pages demos</x-anchor>
        </p>

        <pre x-pre><code>&lt;x-anchor href="/demo/pages" open="auto"&gt;
    Open the Pages demos
&lt;/x-anchor&gt;</code></pre>

        <p>
            <code>open="auto"</code> lets Navigation choose from the current Page context. From this root Page it navigates the top-level destination;
            from a Page already in the stack it updates that stack position.
        </p>

        <h3>Query parameters</h3>

        <p>
            Object properties support state-map attributes. Here <code>query-name</code> and <code>query-count</code> populate
            <code>x-anchor.query</code>, which the component passes to Navigation as query parameters.
        </p>

        <p>
            <x-anchor href="/demo/pages/query-parameters" query-name="lucas" query-count="123">Open the query demo with values</x-anchor>
        </p>

        <pre x-pre><code>&lt;x-anchor
    href="/demo/pages/query-parameters"
    query-name="lucas"
    query-count="123"&gt;
    Open the query demo with values
&lt;/x-anchor&gt;</code></pre>

        <p>
            The destination declares query-backed contract properties, so the Page loader converts these values into its typed state. A controller
            that needs raw query access can instead inject <code>query</code>, a <code>URLSearchParams</code> instance for that Page's own source:
        </p>

        <pre x-pre><code>controller({ query }) {
    return {
        load() {
            const name = query.get("name");
        }
    };
}</code></pre>

        <p>Neither approach parses the browser location directly.</p>

        <h3>Programmatic navigation</h3>

        <p>
            Use the injected Navigation service when a controller decision triggers navigation.
        </p>

        <p>
            <x-button label="Navigate from the controller" command="navigate-programmatically"></x-button>
        </p>

        <pre x-pre><code>controller({ navigation, page }) {
    return {
        "navigate-programmatically"() {
            navigation.navigate({
                href: "/demo/navigation/areas",
                page,
                open: "auto"
            });
        }
    };
}</code></pre>

        <p>
            <code>x-anchor</code> is the declarative choice; <code>navigation.navigate(...)</code> is the programmatic choice. This application
            currently runs Navigation in <strong>{{ state.mode }}</strong> mode, and both examples use the same API.
        </p>

        <h3>Friendly path and canonical href</h3>

        <p>
            A menu item can expose a friendly <code>path</code>, such as <code>/demo/pages/query-parameters</code>, while its <code>href</code> remains
            the canonical XShell Page destination under <code>/_assets/x-demo/...</code>. Areas composes both values and Navigation resolves menu
            paths; <code>x-anchor</code> does not perform that lookup itself.
        </p>
    `,

    state: {
        mode: ""
    },

    controller({ state, navigation, page }) {
        return {
            load() {
                // show the active browser navigation mode
                state.mode = navigation.mode;
            },

            "navigate-programmatically"() {
                // navigate with the controller API
                navigation.navigate({
                    href: "/demo/navigation/areas",
                    page,
                    open: "auto"
                });
            }
        };
    }
};
