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
        <x-tabs>
            <x-tab label="Config">
                <x-page src="config.js" loading="lazy"></x-page>
            </x-tab>
            <x-tab label="Modules">
                Content of Tab 2
            </x-tab>
            <x-tab label="Components">
                Content of Tab 3
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
