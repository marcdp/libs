
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
                
                state.id = page.id; 
                state.label = page.label; 
                state.href = page.src;
                state.path = url.path;
                state.module = url.module;
                state.status = page.host.status;
                state.breadcrumb = page.breadcrumb;
                state.context = page.context || {};
                state.contract = page.contract;
                //state.implementation = page.implementation;


                state.context =  {
                    hello:123,
                    bye: new Date(),
                    other: "world"
                }
                state.count = 123;
                state.count2 = false;

                //alert(JSON.stringify(areas.resolveHref(page.src)));
                
            }
        }
    }
};

