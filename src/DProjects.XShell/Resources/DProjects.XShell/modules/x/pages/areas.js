// contract
export const contract = {
    description: "Shows the available areas for navigation.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    template: `
        <p>
            Please select the area to navigate:
        </p>        
        <x-listview view="icons">
            <x-listview-item 
                x-for="area in state.areas"
                x-class:selected="(state.selected == area.id)"
                x-attr:label="area.label"
                x-attr:description="area.description"
                x-attr:icon="area.icon"
                x-attr:href="area.home"
                open="top"
            ></x-listview-item>
        </x-listview>       
    `,    
    state: {
        areas: {value:[]},
        selected: {value:""}
    },
    script({ state, events, bus, areas }) {
        return {
            load(params) {
                // load
                events.on(bus, "xshell:area:change", "refresh");
                state.areas = areas.getAreas();
                this.onCommand("refresh");
            },

            refresh(params) {
                // refresh selected area
                let currentArea = areas.getCurrentArea();
                if (currentArea) {
                    state.selected = currentArea.id;
                }
            }
        };
    }
}
