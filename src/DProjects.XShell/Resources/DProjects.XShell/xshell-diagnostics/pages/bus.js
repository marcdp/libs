// contract
export const contract = {
    description: "Bus",
    events: {},
    properties: {},
    methods: {}
};

const MAX_EVENTS = 1000;

// export page
export default {
    meta: {
        title: "Bus"
    },
    template: `
        <x-listview ref="listview" view="details" auto-scroll="true">
           <x-datafield slot="column" type="search" x-model="state.query_type" placeholder="Type"></x-datafield>
           <x-datafield slot="column" type="search" x-model="state.query_ts" placeholder="Time" style="width:13em"></x-datafield>
            <x-datafield slot="column" type="search" x-model="state.query_detail" placeholder="Detail"></x-datafield>
            <x-button slot="column" class="plain" command="clear" icon="x-clear" title="Clear list contents"></x-button>
            <x-listview-item x-for="item in state.registry" x-attr:label="item.type" icon="x-thunder" x-show="item.show">
                <x-datetime x-prop:value="item.ts" format="iso" style="width:13em"></x-datetime>
                <x-object x-prop:value="item.detail"></x-object>
                <div></div>
            </x-listview-item>
        </x-listview>        
    `,    
    state:{
        registry: [],
        query_type: "",
        query_ts: "",
        query_detail: ""
    },
    controller({ state, events, bus, page}) {
        return {
            load() {
                // load
                events.on(state, "change:query_type", "refresh");
                events.on(state, "change:query_ts", "refresh");
                events.on(state, "change:query_detail", "refresh");
                events.on(bus, "*", (event)=>{
                    state.registry.push({ 
                        type: event.type,
                        ts: event.ts,
                        detail: event.detail
                    });
                    if (state.registry.length > MAX_EVENTS) state.registry.splice(0, state.registry.length - MAX_EVENTS);
                    this.refresh();
                });
            },
            clear() {
                state.registry = [];
            },
            async refresh() {
                //refresh
                for(let item of state.registry) {
                    let show = true;
                    if (state.query_type && item.type.indexOf(state.query_type) == -1 ) show = false;
                    if (state.query_detail && JSON.stringify(item.detail).indexOf(state.query_detail) == -1 ) show = false;
                    item.show = show;
                }
                page.invalidate();
            }
        };
    }
}
