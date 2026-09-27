// contract
export const contract = {
    description: "Browser file metadata",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    style: `
        .file-demo-result {padding:.75em; border:var(--x-datafield-border); border-radius:var(--x-datafield-border-radius);}
        .file-demo-result p {margin:.25em 0;}
    `,
    template: `
        <h2>Metadata</h2>

        <p>
            A browser <code>File</code> provides a name, byte size, MIME type, and last-modified timestamp immediately after selection.
        </p>

        <p><label>Select a file <input type="file" x-on:change="fileChanged"></label></p>

        <div class="file-demo-result" x-if="state.file">
            <strong>{{ state.file.name }}</strong>
            <p>Name: {{ state.file.name }}</p>
            <p>Size: <x-file-size x-attr:value="state.file.size"></x-file-size></p>
            <p>MIME type: {{ state.file.type }}</p>
            <p>Last modified: {{ state.file.lastModified }}</p>
        </div>
        <p x-else>No file selected.</p>

        <pre x-pre><code>file.name
file.size
file.type
file.lastModified</code></pre>

        <p>
            The MIME type is browser-provided metadata. It is useful for display and picker hints, but it is not authoritative server-side
            validation.
        </p>
    `,
    state: {
        file: null
    },
    controller({ state }) {
        return {
            fileChanged({ event }) {
                // format the File metadata for display
                const file = event.target.files?.[0] ?? null;
                state.file = file ? {
                    name: file.name,
                    size: file.size,
                    type: file.type || "Unknown",
                    lastModified: new Date(file.lastModified).toLocaleString()
                } : null;
            }
        };
    }
};
