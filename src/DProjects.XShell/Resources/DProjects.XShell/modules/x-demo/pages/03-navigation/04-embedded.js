// contract
export const contract = {
    description: "Embedded Page navigation",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Embedded Pages</h2>

        <p>
            An embedded Page renders inside another Page's local outlet. It has its own Page instance, query, controller, and lifecycle, but it does
            not become a normal top-level or stack entry in the browser navigation state.
        </p>

        <p>
            <x-anchor
                href="/demo/_assets/x-demo/pages/index.js"
                open="embed"
                outlet="demo">
                Load the demo landing Page into the outlet
            </x-anchor>
        </p>

        <pre x-pre><code>&lt;x-anchor
    href="/demo/_assets/x-demo/pages/index.js"
    open="embed"
    outlet="demo"&gt;
    Load embedded Page
&lt;/x-anchor&gt;

&lt;x-page outlet="demo"&gt;&lt;/x-page&gt;</code></pre>

        <p>
            Navigation finds the named descendant <code>x-page</code> outlet and sets its Page source. The embedded Page uses the embed layout by
            default. Notice that the main Page stays visible and the browser destination does not change.
        </p>

        <h3>Demo outlet</h3>

        <x-page outlet="demo" style="border:1px red solid"></x-page>

        <h3>Embedded versus stack</h3>

        <p>
            Stack navigation adds a Page to <code>navigation.stack</code> and presents it with the stack layout. Embed navigation updates only the
            named local outlet, so it is suitable for nested Page content that belongs on the current screen.
        </p>
    `
};
