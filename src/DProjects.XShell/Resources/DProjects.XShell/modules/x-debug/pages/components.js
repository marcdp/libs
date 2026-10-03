// contract
export const contract = {
    description: "Modules page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    style: `
    `,
    template: `
        <x-listview view="details">
            <div slot="column" style="width:8em">
                <x-datafield type="search" x-model="state.id" placeholder="Compopnent"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <div slot="column" style="width:6em; text-align:right">
                Size
            </div>
            <div slot="column" style="width:6em;">
                Status
            </div>
            <x-listview-item 
                x-for="component in state.components"
                
                x-attr:label="component.id"
                x-attr:description="component.description"
                x-attr:icon="component.icon"
                target="#root"
            >
                <div>{{ component.label }}</div>                
                <x-file-size x-prop:value="component.size" style="text-align:right"></x-file-size>
                <div>{{ component.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        moduleId: "",
        description: "",
        components: null
    },
    controller({ state, events, config, bus}) {
        return {
            load() {
               // load
               this.refresh();
               events.on(state, "change:id", "refresh");
               events.on(state, "change:moduleId", "refresh");
               events.on(state, "change:description", "refresh");
               events.on(state, "change:label", "refresh");
               events.on(bus, "xshell:loader:resource:loaded", (event)=>{
                    if (event.detail.resource.startsWith("component:")) {
                        this.refresh();
                    }
                });
            },
            refresh() {
                // refresh
                let list = [];
                for(let moduleId of Object.keys(config.modules)) {
                    const moduleConfig = config.modules[moduleId];
                    let moduleComponentsPath = moduleConfig.assetsPath + "/components";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(moduleComponentsPath)) {
                            const filename = (file.path.split("/").pop() || "").split(".")[0];
                            let valid = true;
                            if (state.id && filename.indexOf(state.id) == -1 ) valid = false;
                            if (state.moduleId && moduleId.indexOf(state.moduleId) == -1 ) valid = false;
                            //if (state.description && moduleConfig.description.indexOf(state.description) == -1 ) valid = false;
                            if (valid) {
                                list.push({
                                    id: filename,
                                    version: moduleConfig.version || "",
                                    label: moduleConfig.label || "",
                                    icon: moduleConfig.icon || "x-component",
                                    status: (customElements.get(filename) ? "loaded" : ""),
                                    size: file.size
                                });
                            }
                        }                        
                    }
                };
                state.components = list;
            }
        };
    }
}
