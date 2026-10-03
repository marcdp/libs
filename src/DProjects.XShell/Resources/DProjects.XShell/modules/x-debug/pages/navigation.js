// contract
export const contract = {
    description: "Navigation",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Navigation"
    },
    template: `
        <h2>Areas</h2>
        <x-listview view="details">
            <div slot="column" style="width:10em">
                Area
            </div>
            <div slot="column">
                Prefix
            </div>
            <div slot="column">
                Label
            </div>
            <div slot="column">
                Default
            </div>
            <div slot="column">
                Home
            </div>
            <div slot="column">
                Modules
            </div>
            <x-listview-item x-for="area in state.areas"
                x-attr:href="area.home" 
                x-attr:label="area.id"
                x-attr:icon="area.icon || 'x-area'"
                target="_blank"
            >
                <div>{{ area.prefix }}</div>
                <div>{{ area.label }}</div>
                <div><input type="checkbox" x-model="area.default" disabled></div>
                <div>{{ area.home }}</div>
                <div>{{ area.modules | json_stringify }}</div>

            </x-listview-item>
        </x-listview> 

        
        <h2>Menus</h2>
        <x-listview view="details">
            <div slot="column" style="width:12em">Menu</div>
            <div slot="column">Menuitems</div>
            <x-listview-item x-for="menu in state.menus" x-attr:label="menu.id" x-attr:href="menu.path" target="_blank">
                <x-object x-prop:value="menu.children"></x-object>
            </x-listview-item>
        </x-listview>


        <h2>Page hierarchy</h2>       
        <x-treeview>
            <x-treeview-head>
                <x-treeview-column label="Page" width="30%"></x-treeview-column>
                <x-treeview-column label="Layout" width="8%"></x-treeview-column>
                <x-treeview-column label="Module" width="8%"></x-treeview-column>
                <x-treeview-column label="Loading" width="8%"></x-treeview-column>
                <x-treeview-column label="Path" width="20%"></x-treeview-column>
                <x-treeview-column label="Time" width="5em"></x-treeview-column>
                <x-treeview-column label="Size" width="5em"></x-treeview-column>
                <x-treeview-column label="Status" width="5em"></x-treeview-column>
            </x-treeview-head>
            <x-treeview-body>
                <x-treeview-item x-recursive="item in state.children" x-attr:label="item.label" expanded x-class:error="item.status=='error'">
                    <div slot="column">{{item.layout}}</div>
                    <div slot="column">{{item.module}}</div>
                    <div slot="column">{{item.loading}}</div>
                    <div slot="column" x-attr:title="item.src">{{item.src}}</div>
                    <div slot="column"><x-time-ms x-prop:value="item.time"></x-time-ms></div>
                    <div slot="column"><x-file-size x-prop:value="item.size"></x-file-size></div>
                    <div slot="column">{{item.status}}</div>
                </x-treeview-item>
            </x-treeview-body>
        </x-treeview>   

        
    `,    
    state:{
        areas: null,
        children: [],
        menus: null
    },
    controller({ state, events, config, bus, loader, areas, modules}) {
        return {
            load() {
                // load
                events.on(state, "change:id", "refresh");
                events.on(state, "change:moduleId", "refresh");
                events.on(state, "change:description", "refresh");
                events.on(state, "change:label", "refresh");
                events.on(bus, "xshell:page:load", "refresh");
                events.on(bus, "xshell:loader:resource:loaded", (event)=>{
                    if (event.detail.resource.startsWith("component:")) {
                        this.refresh();
                    }
                });
                this.refresh();
            },
            async refresh() {
                // refresh
                state.areas = areas.getAreas();
                // traverse the DOM to build the page hierarchy
                let traverse = (node, parent) => {
                    if (node.nodeType === Node.ELEMENT_NODE || node.nodeType === Node.DOCUMENT_FRAGMENT_NODE) {
                        if (node.localName === "x-page") {
                            let item = {
                                src: node.src,
                                label: node.label || "?",
                                icon: node.icon,
                                layout: node.layout, 
                                loading: node.loading || '', 
                                status: node.status,
                                module: areas.getModuleId(node.src),
                                time: (node.stats && node.stats.loadTime ? node.stats.loadTime : -1),
                                size: (node.stats && node.stats.loadSize ? node.stats.loadSize : -1),
                                children:[],
                            }
                            parent.children.push(item)
                            parent = item;
                        }
                        for (let child of node.childNodes) {
                            traverse(child, parent);
                        }
                        if (node.shadowRoot) {
                            traverse(node.shadowRoot, parent);
                        }
                    }
                }
                let root = {children:[]}
                traverse(document.body, root)
                state.children = root.children;
                //set menus
                const menus = [];
                for (const area of state.areas) {
                    for( const [menuId, menuItems] of Object.entries(area.menus)) {
                        const menu = {
                            id: area.id + "/" + menuId,
                            children: menuItems,
                        }
                        menus.push(menu);
                    }
                }
                state.menus = menus;
            }
        };
    }
}
