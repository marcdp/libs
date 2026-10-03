// contract
export const contract = {
    description: "Index page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    style: `
        x-tabs {width: 70em; max-width:90vw; }
        x-tab {max-height:50vh; overflow-y:auto;}
    `,
    template: `
        <x-tabs selected-index="7">
            <x-tab label="Config">
                <x-page src="config.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Modules">
                <x-page src="modules.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Components">
                <x-page src="components.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Pages">
                Content of Tab 4
            </x-tab>
            <x-tab label="Areas/Menus">
                Content of Tab 5
            </x-tab>
            <x-tab label="Contracts">
                Content of Tab 6
            </x-tab>
            <x-tab label="Services">
                Content of Tab 7
            </x-tab>
            <x-tab label="Loader">
                <x-page src="loader.js" loading="lazy"></x-page>
            </x-tab>
            
        </x-tabs>
    `,    
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
