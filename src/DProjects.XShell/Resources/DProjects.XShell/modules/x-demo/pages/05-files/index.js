// contract
export const contract = {
    description: "Browser file handling overview",
    events: {},
    properties: {},
    methods: {}
};

// page
export default {
    template: `
        <h2>Files</h2>

        <p>
            XShell Pages run in the browser, so a file selected by the user is a standard browser <code>File</code> object. The browser exposes one
            file as a <code>File</code> and a selection as a <code>FileList</code>.
        </p>

        <pre x-pre><code>user selects file
    ↓
browser File / FileList
    ↓
Page controller
    ↓
inspect / read / upload / process</code></pre>

        <p>
            These pages demonstrate browser-side selection, metadata, reading, multiple-file selection, and client-side downloads. They do not
            upload anything to a server or provide persistent file storage.
        </p>

        <h3>File pages</h3>

        <ul>
            <li><strong>Select</strong> — choose one file and inspect safe display metadata.</li>
            <li><strong>Metadata</strong> — inspect the standard properties of a browser <code>File</code>.</li>
            <li><strong>Read</strong> — read a text file asynchronously with a bounded preview.</li>
            <li><strong>Multiple</strong> — work with a browser <code>FileList</code> as an array of metadata objects.</li>
            <li><strong>Download</strong> — create a <code>Blob</code> and download it in the browser.</li>
        </ul>

        <h3>Files are different from module resources</h3>

        <p>
            A module resource is an application-owned asset resolved by XShell. A browser <code>File</code> is user-selected (or browser-created)
            data. The examples in this section use native browser file APIs and do not use Resolver or Loader for local user files.
        </p>

        <h3>Current file input support</h3>

        <p>
            <code>x-datafield type="file"</code> can render a file input and pass through attributes such as <code>accept</code> and
            <code>multiple</code>, but its higher-level file-value handling is not yet complete. The live examples therefore use native inputs and
            keep native <code>File</code> objects out of reactive state.
        </p>

        <pre x-pre><code>&lt;x-datafield
    type="file"
    accept="text/*"
    multiple&gt;
&lt;/x-datafield&gt;</code></pre>
    `
};
