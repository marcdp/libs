// contract
export const contract = {
    description: "Selecting one browser file",
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
        <h2>Select</h2>

        <p>
            A native file input exposes the selected files through <code>event.target.files</code>. That value is a browser <code>FileList</code>;
            this example reads its first item and copies only display-safe metadata into Page state.
        </p>

        <p><label>Select one file <input type="file" x-on:change="fileChanged"></label></p>

        <div class="file-demo-result" x-if="state.file">
            <strong>Selected file</strong>
            <p>Name: {{ state.file.name }}</p>
            <p>Type: {{ state.file.type }}</p>
            <p>Size: <x-file-size x-attr:value="state.file.size"></x-file-size></p>
        </div>
        <p x-else>No file selected.</p>

        <h3>Native FileList access</h3>

        <pre x-pre><code>&lt;input type="file" x-on:change="fileChanged"&gt;

fileChanged({ event }) {
    const file = event.target.files?.[0] ?? null;
    // keep file local; store plain metadata in reactive state
}</code></pre>

        <p>
            The native <code>File</code> stays controller-local while the rendered state contains ordinary values. This avoids treating browser-native
            objects as serializable application state.
        </p>
    `,
    state: {
        file: null
    },
    controller({ state }) {
        let selectedFile = null;

        return {
            fileChanged({ event }) {
                // copy display-safe metadata from the FileList
                selectedFile = event.target.files?.[0] ?? null;
                state.file = selectedFile ? {
                    name: selectedFile.name,
                    type: selectedFile.type || "Unknown",
                    size: selectedFile.size
                } : null;
            }
        };
    }
};
