// contract
export const contract = {
    description: "Contracts page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Contracts"
    },
    template: `
        <x-listview view="details">
            <x-datafield type="search" x-model="state.id" placeholder="Contract" slot="column" style="width:12em"></x-datafield>
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <x-listview-item x-for="contract in state.contracts"
                x-attr:href="contract.url" 
                x-attr:label="contract.id"
                x-attr:icon="contract.icon"
                target="_blank"
            >
                <div>{{ contract.moduleId }}</div>
                <div>{{ contract.description }}</div>                
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        moduleId: "",
        description: "",
        contracts: null
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
                    let moduleComponentsPath = moduleConfig.assetsPath + "/contracts";
                    for(const file of Object.values(moduleConfig.files)) {
                        if (file.path.startsWith(moduleComponentsPath)) {
                            const id = (file.path.split("/").pop() || "").split(".")[0];
                            const resolved = resolver.resolve("contract:" + id);
                            const contract = await loader.load("contract:" + id);
                            let valid = true;
                            if (state.id && id.indexOf(state.id) === -1) valid = false;
                            if (state.moduleId && moduleId.indexOf(state.moduleId) === -1) valid = false;
                            if (state.description && contract.description.indexOf(state.description) === -1) valid = false;
                            if (state.label && id.indexOf(state.label) === -1) valid = false;
                            if (valid) {
                                list.push({
                                    id: id,
                                    url: resolved.url,
                                    moduleId: moduleId,
                                    description: contract.description
                                });
                            }
                        }                        
                    }
                };
                state.contracts = list;
            }
        };
    }
}
