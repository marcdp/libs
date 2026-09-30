// contract
export const contract = {
    description: "Error page.",
    events: {},
    properties: {},
    methods: {}
};

// implementation
export default {
    template: `
        this is the error page
    `,    
    state: {
    },
    controller({ state, events, bus, areas }) {
        return {
            load() {
                // load
                debugger;
            }
        };
    }
}
