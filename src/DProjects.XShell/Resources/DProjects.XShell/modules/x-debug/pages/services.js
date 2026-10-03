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
            <div slot="column" style="width:12em">
                <x-datafield type="search" x-model="state.contractItemId" placeholder="Contract"></x-datafield>
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
                <x-listview-item x-for="item in state.items"
                    x-attr:href="item.implementationItem ? item.implementationItem.url : item.url" 
                    x-attr:label="item.id"
                    x-attr:icon="item.icon"
                    target="_blank"
                >
                <div>
                    <span x-if="item.implementationItem">
                        {{ item.implementationItem.moduleId }}
                     </span>
                 </div>
                <div>
                    <x-anchor x-if="item.contractItem" x-attr:href="item.contractItem ? item.contractItem.url : ''" target="_blank" class="plain">
                        {{ item.contractItem.id }}
                    </x-anchor>
                 </div>
                <div>{{ item.description }}</div>
                <x-file-size x-prop:value="item.implementationItem ? item.implementationItem.size : 0" style="text-align:right"></x-file-size>
                <div style="text-align:right"><x-time-ms x-prop:value="item.implementationItem ? item.implementationItem.time : 0"></x-time-ms></div>
                <div>{{ item.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        label: "",
        moduleId: "",
        description: "",
        items: null
    },
    controller({ state, events, config, bus, services}) {
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
                let items = services.getServiceItems();
                for (const serviceId in items) {
                    let item = items[serviceId];
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
                        list.push({
                            id: serviceId,
                            icon: item.contractItem ? item.contractItem.contract.icon || "x-service" : "x-service",
                            contractItem: item.contractItem,
                            implementationItem: item.implementationItem,
                            status: "loaded",
                            size: item.implementationItem ? item.implementationItem.size : 0,
                            time: item.implementationItem ? item.implementationItem.time : 0,
                        });
                    }
                }
                state.items = list;
            }
        };
    }
}
