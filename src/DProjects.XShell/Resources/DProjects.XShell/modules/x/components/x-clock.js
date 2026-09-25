// contract
export const contract = {
    description: "Displays the current local time.",
    events: {},
    properties: {},
    methods: {}
};


// definition
export default {
    style: `
        :host {border:1px solid black; display:inline-block; padding:10px;}
    `,
    template: `
        {{ state.time }}
    `,
    state: {
        time: null
    },
    controller: ({ state, timer }) => {
        return {
            load() {
                //load
                this.refresh();
                timer.setInterval(1000, "refresh");
            },

            refresh() {
                //refresh
                state.time = new Date().toLocaleTimeString();
            }
        };
    }
};
