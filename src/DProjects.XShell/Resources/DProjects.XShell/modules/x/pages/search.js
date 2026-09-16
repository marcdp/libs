// export page
export default {
    template: `
        <x-datafields>

            <div x-if="state.results.length > 0">
                <x-listview x-if="state.results.length > 0" view="list" title="Search results">
                    <x-listview-item 
                        x-for="result in state.results"
                        x-attr:label="result.label"
                        x-attr:description="result.description"
                        x-attr:icon="result.logo"
                        x-attr:href="result.href"
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
                    No search results for <b>{{state.keyword}}</b>
                </div>
                <div x-else>
                    Enter at least 3 characters to search
                </div>
            </div>
            
        </x-datafields>    
    `,    
    state: {
        keyword: {value:"", attr:true},
        results: {value:[]}
    },
    script({ state, events, bus, modules, areas, menus, config }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                    // load
                    events.on(bus, "xshell:search", (event) => {
                        state.keyword = event.detail.keyword
                    });                
                    events.on(state, "change:keyword", "search");

                } else if (command == "search") {
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
                        const menu = menus.getMenu(currentArea.id);
                        for(let menuitem of menu) searchRecursive(menuitem);
                        // search other areas
                        // todo ...
                    }
                    state.results = results;
                }                
            }
        };
    }
}
