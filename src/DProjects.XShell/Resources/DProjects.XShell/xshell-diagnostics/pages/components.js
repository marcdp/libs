// contract
export const contract = {
    description: "Components page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Components"
    },
    template: `
        <x-listview view="details">
            <x-datafield type="search" x-model="state.id" placeholder="Component" slot="column" style="width:12em"></x-datafield>
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <div slot="column" style="width:4em; text-align:right">
                Size
            </div>
            <div slot="column" style="width:4em; text-align:right">
                Time
            </div>
            <div slot="column" style="width:4em;">
                Status
            </div>
            <x-listview-item x-for="component in state.components"
                x-attr:href="component.url" 
                x-attr:label="component.id"
                x-attr:icon="component.icon"
                target="_blank"
            >
                <div>{{ component.moduleId }}</div>
                <div>{{ component.description }}</div>
                <x-file-size x-prop:value="component.size" style="text-align:right"></x-file-size>
                <div style="text-align:right"><x-time-ms x-prop:value="component.time"></x-time-ms></div>
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
    controller({ state, events, config, bus, loader}) {
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
            async refresh() {
                // refresh
                let list = [];
                let loaderRegistryCache = {};
                for (const loaderRegistryItem of loader.registry) {
                    loaderRegistryCache[loaderRegistryItem.resource] = loaderRegistryItem;
                }
                for(let moduleId of Object.keys(config.modules)) {
                    const moduleConfig = config.modules[moduleId];
                    let moduleComponentsPath = moduleConfig.assetsPath + "/components";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(moduleComponentsPath)) {
                            const id = (file.path.split("/").pop() || "").split(".")[0];
                            const loaderRegistryItem = loaderRegistryCache["component:" + id];
                            const loaded = (customElements.get(id) != null);
                            const url = new URL(file.path.replace(/^\/+/, ""),config.app.baseUrl).href;
                            let valid = true;
                            if (state.id && id.indexOf(state.id) == -1 ) valid = false;
                            if (state.moduleId && moduleId.indexOf(state.moduleId) == -1 ) valid = false;
                            if (state.description && (!loaderRegistryItem?.description || loaderRegistryItem.description.indexOf(state.description) == -1 )) valid = false;
                            if (valid) {
                                list.push({
                                    id: id,
                                    moduleId: moduleId,
                                    description: loaderRegistryItem?.description || "",
                                    icon: moduleConfig.icon || "x-component",
                                    url: url,
                                    status: (loaded ? "loaded" : ""),
                                    time: loaderRegistryItem?.time || 0,
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
