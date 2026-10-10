// contract
export const contract = {
    description: "Index page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Debug"
    },
    style: `
        x-tabs {width: 80em; max-width:90vw; }
        _:scope {max-height:60vh; overflow-y:auto;}
    `,
    template: `
        <x-tabs use-hash="true">
            <x-tab x-for="menuitem in state.menuitems" x-attr:label="menuitem.label" x-attr:hash="menuitem.label | slug" >
                <x-page x-attr:src="menuitem.href" loading="lazy"></x-page>
            </x-tab>
        </x-tabs>        
    `,    
    state: {
        menuitems: []
    },
    controller({ state, config, areas }) {
        return {
            load() {
               // load
               const area = areas.getArea("xshell-diagnostics");
               const menuitems = area.menus.navigation[0].children;
               state.menuitems = menuitems;
            }
        };
    }
}
