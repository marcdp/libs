// contract
export const contract = {
    description: "Stack Page navigation",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Stack navigation</h2>

        <p>
            <code>open="stack"</code> appends a Page to Navigation's Page stack. XShell renders each additional entry with the stack layout, while the
            main Page remains underneath it.
        </p>

        <p>
            <x-anchor href="/demo/" open="stack" title="Demo landing Page">Open the demo landing Page on the stack</x-anchor>
        </p>

        <pre x-pre><code>&lt;x-anchor
    href="/demo/"
    open="stack"&gt;
    Open the demo landing Page on the stack
&lt;/x-anchor&gt;</code></pre>

        <p>
            The sequence is <strong>main Page → stack Page</strong>. A stack Page can append another entry in the same way. Closing the top panel
            removes that Page from the stack.
        </p>

        <h3>Current navigation stack</h3>

        <ol>
            <li x-for="item in state.stack" x-key="position">
                <code>{{ item.href }}</code>
            </li>
        </ol>

        <p>
            The list uses the public <code>navigation.stack</code> snapshot. Stack Pages are represented in the browser navigation state; embedded and
            dialog Pages are not.
        </p>

        <h3>Automatic behavior inside the stack</h3>

        <p>
            From a root Page, <code>open="auto"</code> navigates the top-level Page. From an existing stack Page, it replaces that stack position and
            leaves earlier entries intact.
        </p>

        <p>
            <x-anchor href="/demo/navigation/areas" open="auto">Navigate this Page position to the Areas demo</x-anchor>
        </p>
    `,

    state: {
        stack: []
    },

    controller({ state, navigation }) {
        return {
            load() {
                // display a stable public snapshot of the logical Page stack
                state.stack = navigation.stack.map((item, index) => ({
                    position: index + 1,
                    href: item.href
                }));
            }
        };
    }
};
