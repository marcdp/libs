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
        <x-toolbar>
            <x-button class="" label="list" command="changeView" data-view="list"></x-button>
            <x-button class="" label="details" command="changeView" data-view="details"></x-button>
            <x-button class="" label="icons" command="changeView" data-view="icons"></x-button>
            <x-button class="" label="tiles" command="changeView" data-view="tiles"></x-button>
        </x-toolbar>

        <x-listview x-attr:view="state.view">
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
                x-attr:description="item.moduleId"
                x-attr:category="item.category"
                target="_blank"
            >
                <div>{{ item.moduleId }}</div>
                <x-file-size x-prop:value="item.size" style="text-align:right"></x-file-size>
                <div style="text-align:right"><x-time-ms x-prop:value="item.time"></x-time-ms></div>
                <div>{{ item.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        view: "tiles",
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
            changeView({view}) {
                state.view = view;
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
                                    category: moduleConfig.label,
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
