export const contract = {
    description: "Dialog service overview",
    events: {},
    properties: {},
    methods: {}
};

export default {
    template: `
        <p>
            XShell dialogs are ordinary Pages presented with the dialog layout. The Dialog service opens the Page and waits for the value
            supplied when that Page closes.
        </p>

        <pre x-pre><code>Dialog service
    ↓
opens a Page with dialog presentation
    ↓
awaits Page completion
    ↓
Page closes with host.close(result)
    ↓
result is returned to the caller</code></pre>

        <h2>Common APIs</h2>

        <p>
            The child pages in this section provide executable examples for
            <code>dialog.message()</code>, <code>dialog.confirm()</code>, <code>dialog.prompt()</code>, <code>dialog.picker()</code>, and
            <code>dialog.open()</code>.
        </p>

        <p>
            Use the normal x-demo navigation to open <strong>Message</strong>, <strong>Prompt</strong>, <strong>Picker</strong>,
            <strong>Results</strong>, and <strong>Custom</strong>.
        </p>
    `
};
