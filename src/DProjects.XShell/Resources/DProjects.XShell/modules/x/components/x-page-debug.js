
// contract
export const contract = {
    description: "Displays the current page diagnostrics debug information.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: `
        :host {}
    `,
    template: `

        <x-propertygrid x-prop:value="state"></x-propertygrid>

        <x-datatable>
            <table>
                <thead>
                    <tr>
                        <th>Key</th>
                        <th>Value</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>id:</td>
                        <td>{{ state.id }}</td>
                    </tr>
                    <tr>
                        <td>label:</td>
                        <td>{{ state.label }}</td>
                    </tr>
                    <tr>
                        <td>href:</td>
                        <td>{{ state.href }}</td>
                    </tr>
                    <tr>
                        <td>path:</td>
                        <td>{{ state.path }}</td>
                    </tr>
                    <tr>
                        <td>module:</td>
                        <td>{{ state.module }}</td>
                    </tr>
                    <tr>
                        <td>context:</td>
                        <td>{{ state.context | json_stringify }}</td>
                    </tr>
                    <tr>
                        <td>status:</td>
                        <td>{{ state.status }}</td>
                    </tr>
                    <tr>
                        <td>breadcrumb:</td>
                        <td>
                        {{ state.breadcrumb | json_stringify }}
                        </td>
                    </tr>
                    <tr>
                        <td>renderEngine:</td>
                        <td>{{ state.implementation.meta.renderEngine }}</td>
                    </tr>
                    <tr>
                        <td>stateEngine:</td>
                        <td>{{ state.implementation.meta.stateEngine }}</td>
                    </tr>
                    <tr>
                        <td>counter:</td>
                        <td>{{ state.count }}</td>
                    </tr>
                </tbody>
            </table>
        </x-datatable>
    `,
    state: {
        id: "",
        label: "",
        href: "",
        path: "",
        module: "",
        status: "",
        breadcrumb: [],
        context: {},
        contract: null,
        implementation: null        
    },
    controller({ state, whenPage, areas, navigation }) {
        return {
            async mount() {
                // mount
                const page = await whenPage();
                const url = areas.resolveHref(page.src);
                console.log(page)
                state.id = page.id; 
                state.label = page.label; 
                state.href = page.src;
                state.path = url.path;
                state.module = url.module;
                state.status = page.host.status;
                state.breadcrumb = page.breadcrumb;
                state.context = page.context || {};
                state.contract = page.contract;
                state.implementation = page.implementation;
                state.count = 123;

                //alert(JSON.stringify(areas.resolveHref(page.src)));
                
            }
        }
    }
};

