// contract
export const contract = {
    description: "Services page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Services"
    },
    template: `
        <x-listview view="details">
            <x-datafield type="search" x-model="state.id" placeholder="Service" slot="column" style="width:12em"></x-datafield>
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <x-listview-item x-for="service in state.services"
                x-attr:href="service.url" 
                x-attr:label="service.id"
                x-attr:icon="service.icon"
                target="_blank"
            >
                <div>{{ service.moduleId }}</div>
                <div>{{ service.description }}</div>                
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        moduleId: "",
        description: "",
        services: null
    },
    controller({ state, events, config, bus, loader, resolver}) {
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
                for(let moduleId of Object.keys(config.modules)) {
                    const moduleConfig = config.modules[moduleId];
                    let moduleComponentsPath = moduleConfig.assetsPath + "/services";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(moduleComponentsPath)) {
                            const id = (file.path.split("/").pop() || "").split(".")[0];
                            const resolved = resolver.resolve("service:" + id);
                            const service = await loader.load("service:" + id);
                            let valid = true;
                            if (state.id && id.indexOf(state.id) === -1) valid = false;
                            if (state.moduleId && moduleId.indexOf(state.moduleId) === -1) valid = false;
                            if (state.description && service.description.indexOf(state.description) === -1) valid = false;
                            if (state.label && id.indexOf(state.label) === -1) valid = false;
                            if (valid) {
                                list.push({
                                    id: id,
                                    url: resolved.url,
                                    moduleId: moduleId,
                                    description: service.description
                                });
                            }
                        }                        
                    }
                };
                state.services = list;
            }
        };
    }
}
