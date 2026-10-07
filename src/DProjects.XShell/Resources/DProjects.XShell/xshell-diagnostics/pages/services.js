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
                    <x-anchor x-if="item.contractUrl" x-attr:href="item.contractUrl" target="_blank" class="plain">
                        {{ item.contractId }}
                    </x-anchor>
                    <span x-if="!item.contractUrl">{{ item.contractId }}</span>
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
    controller({ state, events, services, bus}) {
        return {
            load() {
               // load
               this.refresh();
               events.on(state, "change:id", "refresh");
               events.on(state, "change:moduleId", "refresh");
               events.on(state, "change:contractId", "refresh");
               events.on(state, "change:description", "refresh");
               events.on(bus, "xshell:service:created", "refresh");
            },
            async refresh() {
                // refresh
                const list = [];
                for (const item of services.registry) {
                    const contractId = item.contractId || "";
                    const description = item.description || (item.url
                        ? `Configured service (${item.implementationName || "implementation"}).`
                        : "Runtime service.");
                    const moduleId = item.moduleId || (item.url ? "-" : "runtime");
                    let valid = true;
                    if (state.id && !item.id.includes(state.id)) {
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
                            serviceId: item.id,
                            icon: item.icon || "x-service",
                            contractUrl: item.contractUrl,
                            contractId: contractId || "-",
                            description: description,
                            moduleId: moduleId,
                            url: item.url,
                            status: item.state,
                            size: item.size ?? 0,
                            time: item.time ?? 0,
                        });
                    }
                }
                state.items = list;
            }
        };
    }
}
