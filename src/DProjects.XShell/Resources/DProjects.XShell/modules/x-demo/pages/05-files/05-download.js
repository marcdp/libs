// contract
export const contract = {
    description: "Creating a client-side file download",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    style: `
        .file-demo-download {display:flex; flex-direction:column; gap:.75em; max-width:40em;}
        .file-demo-download textarea {min-height:10em; width:100%; box-sizing:border-box;}
    `,
    template: `
        <h2>Download</h2>

        <p>
            This page creates a file entirely in the browser. Page data becomes a <code>Blob</code>, the Blob becomes an object URL, and an anchor
            with a <code>download</code> name starts the download.
        </p>

        <div class="file-demo-download">
            <label>
                File contents
                <textarea x-model="state.content"></textarea>
            </label>
            <button x-on:click="download">Download example.txt</button>
            <p>{{ state.message }}</p>
        </div>

        <pre x-pre><code>Page data
    ↓
Blob
    ↓
object URL
    ↓
download</code></pre>

        <pre x-pre><code>const blob = new Blob([content], {type: "text/plain"});
const url = URL.createObjectURL(blob);
const anchor = document.createElement("a");
anchor.href = url;
anchor.download = "example.txt";
anchor.click();
window.setTimeout(() =&gt; URL.revokeObjectURL(url), 0);</code></pre>

        <p>
            No server or upload endpoint is involved. The object URL is revoked after the browser has received the download request.
        </p>
    `,
    state: {
        content: "Hello from an XShell Page!\n",
        message: ""
    },
    controller({ state }) {
        return {
            download() {
                // create and release a browser-only download URL
                const blob = new Blob([state.content], {type: "text/plain"});
                const url = URL.createObjectURL(blob);
                const anchor = document.createElement("a");
                anchor.href = url;
                anchor.download = "example.txt";
                document.body.appendChild(anchor);
                anchor.click();
                anchor.remove();
                window.setTimeout(() => URL.revokeObjectURL(url), 0);
                state.message = "Download requested for example.txt.";
            }
        };
    }
};
