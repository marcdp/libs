// contract
export const contract = {
    description: "Reading text from a browser file",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    style: `
        .file-demo-result {padding:.75em; border:var(--x-datafield-border); border-radius:var(--x-datafield-border-radius);}
        .file-demo-result p {margin:.25em 0;}
        .file-demo-preview {max-height:20em; overflow:auto; white-space:pre-wrap; overflow-wrap:anywhere;}
    `,
    template: `
        <h2>Read</h2>

        <p>
            File metadata is available immediately. File contents require asynchronous I/O, so this page uses <code>await file.text()</code> and
            renders only the first 2,000 characters.
        </p>

        <p><label>Select a text-oriented file <input type="file" accept="text/*,.md,.csv,.json" x-on:change="fileChanged"></label></p>
        <p>{{ state.message }}</p>

        <div class="file-demo-result" x-if="state.file">
            <p>Name: {{ state.file.name }}</p>
            <p>Size: <x-file-size x-attr:value="state.file.size"></x-file-size></p>
            <h3>Content preview</h3>
            <pre class="file-demo-preview" x-text="state.preview"></pre>
            <p x-if="state.truncated">Preview limited to the first 2,000 characters.</p>
        </div>

        <pre x-pre><code>const file = event.target.files?.[0] ?? null;
const content = await file.text();
state.preview = content.slice(0, 2000);</code></pre>
    `,
    state: {
        file: null,
        message: "No file selected.",
        preview: "",
        truncated: false
    },
    controller({ state }) {
        let selectedFile = null;

        return {
            async fileChanged({ event }) {
                // read only the currently selected text file
                const file = event.target.files?.[0] ?? null;
                selectedFile = file;
                state.file = file ? {name: file.name, size: file.size} : null;
                state.preview = "";
                state.truncated = false;

                if (!file) {
                    state.message = "No file selected.";
                    return;
                }

                state.message = "Reading file contents...";
                try {
                    const content = await file.text();
                    if (selectedFile !== file) return;
                    state.preview = content.slice(0, 2000);
                    state.truncated = content.length > 2000;
                    state.message = "File read successfully.";
                } catch (error) {
                    if (selectedFile !== file) return;
                    state.message = "The file could not be read as text.";
                }
            }
        };
    }
};
