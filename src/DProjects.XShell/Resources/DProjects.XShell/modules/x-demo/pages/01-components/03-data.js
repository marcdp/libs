// contract
export const contract = {
    description: "Data page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <h1>Data components</h1>

        <p>
            Components for displaying collections, structured data, dates, durations, and file sizes.
        </p>

        <h2>List view</h2>

        <p>
            List view supports list, icon, and details presentations.
        </p>

        <h3>List</h3>

        <x-listview view="list">

            <x-listview-item icon="x-file" label="Document.txt" description="Text document"> </x-listview-item>
            <x-listview-item icon="x-image" label="Photo.jpg" description="Image"> </x-listview-item>
            <x-listview-item icon="x-folder" label="Projects" description="Folder"> </x-listview-item>

        </x-listview>

        <h3>Icons</h3>

        <x-listview view="icons">

            <x-listview-item icon="x-file" label="Document"></x-listview-item>
            <x-listview-item icon="x-image" label="Photo"></x-listview-item>
            <x-listview-item icon="x-folder" label="Projects"></x-listview-item>

        </x-listview>

        <h3>Details</h3>

        <x-listview view="details">

            <span slot="column">Name</span>
            <span slot="column">Type</span>
            <span slot="column">Size</span>

            <x-listview-item icon="x-file" label="Document.txt">
                <span>Text document</span>
                <span>24 KB</span>
            </x-listview-item>

            <x-listview-item icon="x-image" label="Photo.jpg">
                <span>Image</span>
                <span>2.4 MB</span>
            </x-listview-item>

            <x-listview-item icon="x-folder" label="Projects">
                <span>Folder</span>
                <span>-</span>
            </x-listview-item>

        </x-listview>

        <x-divider></x-divider>

        <h2>Tree view</h2>

        <x-treeview>

            <x-treeview-item label="Documents" icon="x-folder" has-childs expanded>
                <x-treeview-item label="Reports" icon="x-folder"has-childs>
                    <x-treeview-item label="Reportttt.pdf" icon="x-file"></x-treeview-item>
                </x-treeview-item>
                <x-treeview-item label="Notes.txt" icon="x-file"></x-treeview-item>
            </x-treeview-item>
            <x-treeview-item label="Images" icon="x-folder" has-childs>
                <x-treeview-item label="Photo.jpg" icon="x-image"></x-treeview-item>
            </x-treeview-item>

        </x-treeview>


        <x-divider></x-divider>

        <h2>Data table</h2>

        <x-datatable>
            <table>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Status</th>
                        <th>Items</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Project A</td>
                        <td>Active</td>
                        <td>12</td>
                    </tr>
                    <tr>
                        <td>Project B</td>
                        <td>Paused</td>
                        <td>5</td>
                    </tr>
                    <tr>
                        <td>Project C</td>
                        <td>Completed</td>
                        <td>27</td>
                    </tr>
                </tbody>
            </table>
        </x-datatable>

        <x-divider></x-divider>

        <h2>JSON</h2>

        <x-json
            value='{"name":"XShell","enabled":true,"version":1,"items":["one","two","three"]}'>
        </x-json>

        <x-divider></x-divider>

        <h2>Date and time</h2>

        <p>
            Default:
            <x-datetime datetime="2026-09-25T10:30:00Z"></x-datetime>
        </p>

        <p>
            Date:
            <x-datetime
                datetime="2026-09-25T10:30:00Z"
                format="date">
            </x-datetime>
        </p>

        <p>
            Time:
            <x-datetime
                datetime="2026-09-25T10:30:00Z"
                format="time">
            </x-datetime>
        </p>

        <x-divider></x-divider>

        <h2>File sizes</h2>

        <p>
            512 bytes:
            <x-file-size value="512"></x-file-size>
        </p>

        <p>
            16 KB:
            <x-file-size value="16384"></x-file-size>
        </p>

        <p>
            5 MB:
            <x-file-size value="5242880"></x-file-size>
        </p>

        <x-divider></x-divider>

        <h2>Durations</h2>

        <p>
            <x-time-ms value="12"></x-time-ms>
        </p>

        <p>
            <x-time-ms value="250"></x-time-ms>
        </p>

        <p>
            <x-time-ms value="1500"></x-time-ms>
        </p>
    `,  
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
