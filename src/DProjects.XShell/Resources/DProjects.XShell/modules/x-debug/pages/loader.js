// contract
export const contract = {
    description: "Loader page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <x-listview view="details" auto-scroll>
            <div slot="column">
                <x-datafield type="search" x-model="state.query_resource" placeholder="Resource"></x-datafield>
            </div>
            <div slot="column" style="width:6em;">
                Module
            </div>
            <div slot="column" style="width:6em; text-align:right;">
                Time
            </div>
            <div slot="column" style="width:4em;">
                Status
            </div>
            <div slot="column">
                <x-button class="plain" command="clear" icon="x-clear" title="Clear list contents"></x-button>
            </div>
            <x-listview-item x-for="item in state.registry" x-attr:label="item.resource" icon="x-file" x-show="item.show" x-attr:href="item.url" target="_blank">
                <div>{{ item.moduleId }}</div>
                <div style="text-align:right">
                    <x-time-ms x-prop:value="item.time"></x-time-ms>
                </div>
                <div>{{ item.status }}</div>
            </x-listview-item>
        </x-listview>        
    `,    
    state:{
        registry: [],
        query_resource: "",
        query_url: "",
        query_status: ""
    },
    controller({ state, events, loader, bus, page }) {
        return {
            load() {
                // load
                events.on(state, "change:query_resource", "refresh");
                events.on(state, "change:query_url", "refresh");
                events.on(state, "change:query_status", "refresh");

                // setup registry of already loaded resources
                let register = (detail) => {
                    state.registry.push({
                        resource: detail.resource,
                        url: detail.url,
                        status: detail.status,
                        time: detail.time,
                        moduleId: detail.moduleId,
                        show: true,
                    });
                }
                for(let loaderRegistryItem of loader.registry) {
                    register(loaderRegistryItem);
                }
                // listen to loader events
                events.on(bus, "xshell:loader:resource:fetch", (event)=>{
                    register(event.detail)
                    this.refresh();
                });
                events.on(bus, "xshell:loader:resource:loaded", (event)=>{
                    for (let item of state.registry) {
                        if (item.resource == event.detail.resource) {
                            item.status = "loaded";
                            item.time = event.detail.time;
                            break;
                        }
                    }
                    this.refresh();
                });
                events.on(bus, "xshell:loader:resource:error", (event)=>{
                    for (let item of state.registry) {
                        if (item.resource == event.detail.resource) {
                            item.status = "error";
                            item.time = event.detail.time;
                            break;
                        }
                    }
                    this.refresh();
                });
            },
            clear() {
                state.registry = [];
            },
            refresh() {
                // refresh
                for(let item of state.registry) {
                    let show = true;
                    if (state.query_resource && item.resource.indexOf(state.query_resource) == -1 ) show = false;
                    if (state.query_url && item.url.indexOf(state.query_url) == -1 ) show = false;
                    if (state.query_status && item.status.indexOf(state.query_status) == -1 ) show = false;
                    item.show = show;
                }
                page.invalidate();
            }
        };
    }
}
