
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
        :host {display:block;}
        summary:hover {text-decoration:underline; cursor:pointer;}
        x-propertygrid {max-height:50vh; overflow-y:auto;}
        x-icon {vertical-align:bottom;}
    `,
    template: `
        <x-details>
            <div slot="summary">
                <x-icon icon="x-page"></x-icon>
                {{ state.href }}
            </div>
            <x-propertygrid x-prop:value="state"></x-propertygrid>
        </x-details>
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
    controller({ events, bus, state, getPage, areas }) {
        return {
            async load() {
                // mount
                events.on(bus, "xshell:page:load", (event)=> {
                    let page = getPage();
                    if (event.detail.id == page?.id) {
                        this.refresh();
                    }
                });
            },  
            mount(){
                let page = getPage();
                if (page!=null) this.refresh();
            },
            refresh() {
                const page = getPage();
                const url = areas.resolveHref(page.src);   
                state.id = page.id; 
                state.label = page.label; 
                state.href = page.src;
                state.path = url?.path;
                state.module = url?.module ?? areas.getModuleId(page.src);
                state.status = page.host.status;
                state.breadcrumb = page.breadcrumb;
                state.context = page.context || {};
                state.contract = page.contract;
                state.meta = page.implementation?.meta || {};
            }
        }
    }
};

