
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
                        <td>href</td>
                        <td>{{ state.href }}</td>
                    </tr>
                    <tr>
                        <td>Contract</td>
                        <td><x-json x-prop:value="state.contract"></x-json></td>
                    </tr>
                    <tr>
                        <td>Implementation</td>
                        <td><x-json x-prop:value="state.implementation"></x-json></td>  
                    </tr>
                </tbody>
            </table>
        </x-datatable>
    `,
    state: {
        title: "",
        icon: "",      
        href: "",
        hrefReal:"",
        path: "",
        query: {},
        context: {},
        contract: null,
        implementation: null,

    },
    controller({ state, whenPage }) {
        return {
            async mount() {
                // mount
                const page = await whenPage();
                state.title = page.title || "";
                state.icon = page.icon || "";
                state.href = page.href || "";
                state.hrefReal = page.hrefReal || "";
                state.path = page.path || "";
                state.query = page.query || {};
                state.context = page.context || {};
                state.contract = page.contract;
                state.implementation = page.implementation;
            }
        }
    }
};

