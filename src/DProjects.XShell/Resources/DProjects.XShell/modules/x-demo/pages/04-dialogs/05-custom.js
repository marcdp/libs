export const contract = {
    description: "Custom Page dialogs",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            <code>dialog.open()</code> is the general mechanism. It opens any Page with dialog presentation, passes caller data through
            <code>context</code>, and resolves when that Page calls <code>host.close(result)</code>.
        </p>

        <h2>Parent Page</h2>

        <x-datafields columns="2">
            <x-datafield
                type="text"
                label="Context message"
                x-model="state.contextMessage">
            </x-datafield>

            <x-datafield
                type="text"
                label="Initial value"
                x-model="state.initialValue">
            </x-datafield>
        </x-datafields>

        <p>
            <x-button label="Open custom dialog" command="open-custom" class="submit"></x-button>
        </p>

        <pre x-pre><code>const result = await dialog.open({
    href: "custom-dialog.js",
    title: "Custom dialog",
    context: {
        message: "Hello from the parent Page"
    }
});</code></pre>

        <x-divider></x-divider>

        <h2>Result</h2>

        <p x-text="state.resultText"></p>
    `,
    state: {
        contextMessage: "Hello from the parent Page",
        initialValue: "A value from the parent",
        resultText: "No custom dialog has been opened yet."
    },
    controller({ state, dialog, page }) {
        return {
            async "open-custom"() {
                const pageDirectory = page.src.substring(0, page.src.lastIndexOf("/") + 1);
                const result = await dialog.open({
                    href: pageDirectory + "custom-dialog.js",
                    title: "Custom dialog",
                    context: {
                        message: state.contextMessage,
                        value: state.initialValue
                    }
                });
                state.resultText = result === null ? "null — the custom dialog was cancelled." : JSON.stringify(result);
            }
        };
    }
};
