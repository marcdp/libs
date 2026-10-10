
// contract
export const contract = {
    description: "Displays diagnostic information about the current Page",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: `
        :host {display:block;}
        summary:hover {text-decoration:underline; cursor:pointer;}
        x-propertygrid {margin-bottom:.5em;}
        x-icon {vertical-align:bottom;}
    `,
    template: `
        <x-details x-if="state.visible">
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
        implementation: null,
        visible: false
    },
    controller({ events, config, bus, state, getPage, areas }) {
        return {
            async load() {
                // mount
                state.visible = (config.xshell.environment.toLowerCase() === "developmenta");
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

