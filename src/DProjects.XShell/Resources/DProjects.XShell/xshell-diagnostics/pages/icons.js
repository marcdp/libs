// contract
export const contract = {
    description: "Icons page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Icons"
    },
    template: `
        <x-listview view="icons">
            <x-datafield type="search" x-model="state.id" placeholder="Icon" slot="column" style="width:12em"></x-datafield>
            <div slot="column" >
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
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
            <x-listview-item x-for="item in state.items"
                x-attr:href="item.url" 
                x-attr:label="item.id"
                x-attr:icon="item.id"
                target="_blank"
            >
                <!--
                    <div>{{ item.moduleId }}</div>
                    <div>{{ item.description }}</div>
                    <x-file-size x-prop:value="item.size" style="text-align:right"></x-file-size>
                    <div style="text-align:right"><x-time-ms x-prop:value="item.time"></x-time-ms></div>
                    <div>{{ item.status }}</div>
                -->
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        moduleId: "",
        items: null
    },
    controller({ state, events, config, bus, loader}) {
        return {
            load() {
               // load
               this.refresh();
               events.on(state, "change:id", "refresh");
               events.on(state, "change:moduleId", "refresh");
               events.on(bus, "xshell:loader:resource:loaded", (event)=>{
                    if (event.detail.resource.startsWith("icon:")) {
                        this.refresh();
                    }
                });
            },
            async refresh() {
                // refresh
                let items = [];
                let loaderRegistryCache = {};
                for (const loaderRegistryItem of loader.registry) {
                    loaderRegistryCache[loaderRegistryItem.resource] = loaderRegistryItem;
                }
                for(let moduleId of Object.keys(config.modules)) {
                    const moduleConfig = config.modules[moduleId];
                    let moduleIconsPath = moduleConfig.assetsPath + "/icons";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(moduleIconsPath)) {
                            const id = (file.path.split("/").pop() || "").split(".")[0];
                            const url = config.app.basePath + file.path;
                            await loader.load("icon:" + id);
                            const loaderRegistryItem = loaderRegistryCache["icon:" + id];
                            let valid = true;
                            if (state.id && id.indexOf(state.id) == -1 ) valid = false;
                            if (state.moduleId && moduleId.indexOf(state.moduleId) == -1 ) valid = false;
                            if (valid) {
                                items.push({
                                    id: id,
                                    url: url,
                                    moduleId: moduleId,
                                    status: "loaded",
                                    time: loaderRegistryItem?.time || 0,
                                    size: file.size
                                });
                            }
                        }                        
                    }
                };
                state.items = items;
            }
        };
    }
}
