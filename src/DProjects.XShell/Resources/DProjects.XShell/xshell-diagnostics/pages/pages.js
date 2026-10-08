// contract
export const contract = {
    description: "Pages",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Pages"
    },
    template: `
        <x-listview view="details">
            <x-datafield slot="column" type="search" x-model="state.id" placeholder="Page"></x-datafield>
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <div slot="column" style="width:4em; text-align:right">
                Size
            </div>
            <div slot="column" style="width:4em;text-align:right;">
                Time
            </div>
            <div slot="column" style="width:4em;">
                Status
            </div>
            <x-listview-item x-for="page in state.pages"
                x-attr:href="page.url" 
                x-attr:label="page.id"
                x-attr:icon="page.icon"
                target="_blank"
            >
                <div>{{ page.moduleId }}</div>
                <div>{{ page.description }}</div>
                <x-file-size x-prop:value="page.size" style="text-align:right"></x-file-size>
                <div style="text-align:right"><x-time-ms x-prop:value="page.time"></x-time-ms></div>
                <div>{{ page.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        moduleId: "",
        description: "",
        pages: null
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
                    if (event.detail.resource.startsWith("page:")) {
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
                    let modulePagesPath = moduleConfig.assetsPath + "/pages";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(modulePagesPath)) {
                            const id = file.path;
                            const loaderRegistryItem = loaderRegistryCache["page:" + file.path];
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
                                    icon: moduleConfig.icon || "x-page",
                                    url: url,
                                    status: loaderRegistryItem?.status || "",
                                    time: loaderRegistryItem?.time || "",
                                    size: file.size
                                });
                            }
                        }                        
                    }
                };
                state.pages = list;
            }
        };
    }
}
