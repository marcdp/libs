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
                <x-datafield type="search" x-model="state.contractId" placeholder="Contract"></x-datafield>
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
                    x-attr:href="item.url"
                    x-attr:label="item.serviceId"
                    x-attr:icon="item.icon"
                    target="_blank"
                >
                <div>{{ item.moduleId }}</div>
                <div>
                    <x-anchor x-if="item.contractItem" x-attr:href="item.contractItem ? item.contractItem.url : ''" target="_blank" class="plain">
                        {{ item.contractId }}
                    </x-anchor>
                    <span x-if="!item.contractItem">{{ item.contractId }}</span>
                 </div>
                <div>{{ item.description }}</div>
                <x-file-size x-prop:value="item.size" style="text-align:right"></x-file-size>
                <div style="text-align:right"><x-time-ms x-prop:value="item.time"></x-time-ms></div>
                <div>{{ item.status }}</div>
            </x-listview-item>
        </x-listview> 
    `,    
    state:{
        id: "",
        moduleId: "",
        contractId: "",
        description: "",
        items: []
    },
    controller({ state, events, services}) {
        return {
            load() {
               // load
               this.refresh();
               events.on(state, "change:id", "refresh");
               events.on(state, "change:moduleId", "refresh");
               events.on(state, "change:contractId", "refresh");
               events.on(state, "change:description", "refresh");
            },
            async refresh() {
                // refresh
                const list = [];
                const items = services.getServiceItems();
                for (const serviceId in items) {
                    const item = items[serviceId];
                    const contractId = item.contractItem?.id || "";
                    const description = item.contractItem?.contract?.description || (item.implementationItem
                        ? `Configured service (${item.implementationItem.class?.name || "implementation"}).`
                        : `Runtime service (${item.instance?.constructor?.name || "instance"}).`);
                    const moduleId = item.implementationItem ? item.implementationItem.moduleId || "-" : "runtime";
                    let valid = true;
                    if (state.id && !serviceId.includes(state.id)) {
                        valid = false;
                    }
                    if (state.contractId && !contractId.includes(state.contractId)) {
                        valid = false;
                    }
                    if (state.description && !description.includes(state.description)) {
                        valid = false;
                    }
                    if (state.moduleId && !moduleId.includes(state.moduleId)) {
                        valid = false;
                    }
                    if (valid) {
                        list.push({
                            serviceId: serviceId,
                            icon: item.contractItem ? item.contractItem.contract.icon || "x-service" : "x-service",
                            contractItem: item.contractItem,
                            implementationItem: item.implementationItem,
                            contractId: contractId || "-",
                            description: description,
                            moduleId: moduleId,
                            url: item.implementationItem?.url || null,
                            status: item.state,
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
