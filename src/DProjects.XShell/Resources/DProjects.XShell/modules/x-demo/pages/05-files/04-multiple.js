// contract
export const contract = {
    description: "Selecting multiple browser files",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    style: `
        .file-demo-table {border-collapse:collapse; width:100%;}
        .file-demo-table th, .file-demo-table td {padding:.4em; border-bottom:var(--x-datafield-border); text-align:left;}
    `,
    template: `
        <h2>Multiple</h2>

        <p>
            With the <code>multiple</code> attribute, the input still exposes a browser <code>FileList</code>. Convert it to an array and map each
            native file to a small plain object before putting it into reactive state.
        </p>

        <p><label>Select multiple files <input type="file" multiple accept="text/*,.md,.csv,.json" x-on:change="filesChanged"></label></p>

        <p>Files selected: {{ state.count }}</p>
        <p>Total size: <x-file-size x-attr:value="state.totalSize"></x-file-size></p>

        <table class="file-demo-table" x-if="state.files.length">
            <thead>
                <tr><th>Name</th><th>Type</th><th>Size</th></tr>
            </thead>
            <tbody>
                <tr x-for="file in state.files" x-key="id">
                    <td>{{ file.name }}</td>
                    <td>{{ file.type }}</td>
                    <td><x-file-size x-attr:value="file.size"></x-file-size></td>
                </tr>
            </tbody>
        </table>
        <p x-else>No files selected.</p>

        <pre x-pre><code>const files = Array.from(event.target.files || []);
state.files = files.map(file =&gt; ({
    name: file.name,
    type: file.type || "Unknown",
    size: file.size
}));</code></pre>

        <p>
            <code>accept</code> changes the file-picker experience; it is not a security boundary or definitive validation mechanism.
        </p>
    `,
    state: {
        files: [],
        count: 0,
        totalSize: 0
    },
    controller({ state }) {
        return {
            filesChanged({ event }) {
                // copy metadata from the FileList into render-friendly state
                const files = Array.from(event.target.files || []);
                state.files = files.map((file, index) => ({
                    id: `${file.name}-${file.lastModified}-${index}`,
                    name: file.name,
                    type: file.type || "Unknown",
                    size: file.size
                }));
                state.count = state.files.length;
                state.totalSize = state.files.reduce((total, file) => total + file.size, 0);
            }
        };
    }
};
