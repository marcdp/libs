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
                <x-datafield type="search" x-model="state.label" placeholder="Label"></x-datafield>
            </div>
            <div slot="column">
                <x-datafield type="search" x-model="state.description" placeholder="Description"></x-datafield>
            </div>
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.moduleId" placeholder="Module"></x-datafield>
            </div>
            <div slot="column" style="width:4em">
                Size
            </div>
            <div slot="column" style="width:4em">
                Time
            </div>
            <div slot="column" style="width:8em">
                Status
            </div>
            <x-listview-item x-for="contractItem in state.contractItems"
                x-attr:href="contractItem.url" 
                x-attr:label="contractItem.id"
                x-attr:icon="contractItem.icon"
                target="_blank"
            >
                <div>{{ contractItem.contract.label }}</div>
                <div>{{ contractItem.contract.description }}</div>
                <div>{{ contractItem.moduleId }}</div>
                <x-file-size x-prop:value="contractItem.size" style="text-align:right"></x-file-size>
                <x-time-ms x-prop:value="contractItem.time"></x-time-ms>
                <div>{{ contractItem.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        description: "",
        moduleId: "",
        contractItems: null
    },
    controller({ state, events, config, bus, loader, resolver, contracts}) {
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
                const contractItems = [];
                for (const item of Object.values(contracts.getContracts())) {
                    let valid = true;
                    if (state.id && !item.id.includes(state.id)) {
                        valid = false;
                    }
                    if (state.label && !item.contract.label.includes(state.label)) {
                        valid = false;
                    }
                    if (state.description && !item.contract.description.includes(state.description)) {
                        valid = false;
                    }
                    if (state.moduleId && !item.moduleId.includes(state.moduleId)) {
                        valid = false;
                    }
                    if (valid) {
                        contractItems.push(item);
                    }
                }
                state.contractItems = contractItems;
            }
        };
    }
}
