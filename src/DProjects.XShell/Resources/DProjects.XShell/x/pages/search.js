// contract
export const contract = {
    description: "Shows a search page.",
    events: {},
    properties: {},
    methods: {}
};

// implementation
export default {
    meta: {
        title: "Page search"
    },
    template: `
        <x-datafields>

            <div x-if="state.results.length > 0">
                <x-listview x-if="state.results.length > 0" view="list" title="Search results">
                    <x-listview-item 
                        x-for="result in state.results"
                        x-attr:label="result.label"
                        x-attr:description="result.description"
                        x-attr:icon="result.logo"
                        x-attr:href="result.path || result.href"
                        x-attr:target="result.target"
                        x-attr:category="result.category"                    
                        open="top"
                    ></x-listview-item>
                </x-listview>       
                <div>
                    <br>
                    <b>{{ state.results.length }}</b> results found for <b>{{state.keyword}}</b>
                </div>
            </div>

            <div x-else>
                <div x-if="state.keyword.length > 2">
                    <x-empty icon="x-search" label="No results" message="No matching pages were found."></x-empty>
                </div>
                <div x-else>
                    Enter at least 3 characters to search
                </div>
            </div>
            
        </x-datafields>    
    `,    
    state: {
        keyword: "",
        results: []
    },
    controller({ state, events, bus, areas }) {
        return {
            load() {
                // load
                events.on(bus, "xshell:search", (event) => {
                    state.keyword = event.detail.keyword
                });
                events.on(state, "change:keyword", "search");
            },

            search() {
                // search
                let keyword = state.keyword.toLowerCase();
                let results = [];
                if (keyword.length > 2) {
                    const searchRecursive = function(menuitem) {
                        if (menuitem.label.toLowerCase().indexOf(keyword) >= 0 && menuitem.href) {
                            results.push(menuitem);
                        }
                        if (menuitem.children) {
                            for (let child of menuitem.children) {
                                searchRecursive(child);
                            }
                        }
                    }
                    // search current area
                    const currentArea = areas.getCurrentArea();
                    const menu = areas.getMenu("navigation", currentArea.id);
                    for(let menuitem of menu) searchRecursive(menuitem);
                    // search other areas
                    // todo ...
                }
                state.results = results;
            }
        };
    }
}
