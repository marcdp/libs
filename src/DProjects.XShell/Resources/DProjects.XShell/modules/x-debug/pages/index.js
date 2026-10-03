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
        x-tab {max-height:60vh; aoverflow-y:auto;}
    `,
    template: `
        <x-tabs selected-index="0">
            <x-tab label="Config">
                <x-page src="config.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Application">
                <x-page src="app.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Modules">
                <x-page src="modules.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Components">
                <x-page src="components.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Pages">
                <x-page src="pages.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Navigation">
                <x-page src="navigation.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Contracts">
                <x-page src="contracts.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Services">
                <x-page src="services.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Loader">
                <x-page src="loader.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Bus">
                <x-page src="bus.js" loading="lazy"></x-page>
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
