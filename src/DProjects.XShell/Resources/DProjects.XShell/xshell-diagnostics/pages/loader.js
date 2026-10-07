// contract
export const contract = {
    description: "Loader page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Loader"
    },
    template: `
        <x-listview view="details" auto-scroll>
            <div slot="column">
                <x-datafield type="search" x-model="state.query_resource" placeholder="Resource"></x-datafield>
            </div>
            <div slot="column" style="width:6em;">
            <x-datafield type="search" x-model="state.query_module" placeholder="Module"></x-datafield>
            </div>
            <div slot="column" style="width:6em; text-align:right;">
                Load time
            </div>
            <div slot="column" style="width:4em;">
                Status
            </div>
            <div slot="column">
                <x-button class="plain" command="clear" icon="x-clear" title="Clear list contents"></x-button>
            </div>
            <x-listview-item x-for="item in state.items" x-attr:label="item.resource" icon="x-file" x-attr:href="item.url" target="_blank">
                <div>{{ item.moduleId }}</div>
                <div style="text-align:right"><x-time-ms x-prop:value="item.time"></x-time-ms></div>
                <div>{{ item.status }}</div>
            </x-listview-item>
        </x-listview>        
    `,    
    state:{
        items: [],
        query_resource: "",
        query_module: "",
        query_url: "",
        query_status: ""
    },
    controller({ state, events, loader, bus, page }) {
        let clearedCount = 0;
        return {
            load() {
                // load
                events.on(state, "change:query_resource", "refresh");
                events.on(state, "change:query_module", "refresh");
                events.on(state, "change:query_url", "refresh");
                events.on(state, "change:query_status", "refresh");

                // re-read the loader snapshot whenever tracked resources change
                this.refresh();
                events.on(bus, "xshell:loader:resource:fetch", "refresh");
                events.on(bus, "xshell:loader:resource:loaded", "refresh");
                events.on(bus, "xshell:loader:resource:error", "refresh");
            },
            clear() {
                clearedCount = loader.registry.length;
                this.refresh();
            },
            refresh() {
                // derive the visible rows from current loader state and local filters
                state.items = loader.registry.slice(clearedCount).filter(item =>
                    (!state.query_resource || item.resource.includes(state.query_resource)) &&
                    (!state.query_module || item.moduleId?.includes(state.query_module)) &&
                    (!state.query_url || item.url.includes(state.query_url)) &&
                    (!state.query_status || item.status.includes(state.query_status))
                );
                page.invalidate();
            }
        };
    }
}
