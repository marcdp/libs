
// contract
export const contract = {
    description: "Displays the current page description.",
    events: {},
    properties: {},
    methods: {}
};


// definition
export default {
    style: ``,
    template: `{{ state.description }}`,
    state: {
        description: ""
    },
    controller({ events, bus, state, getPage }) {
        return {
            load(...args) {
                // load
                events.on(bus, "xshell:page:load", (event)=> {
                    if (event.detail.id == getPage()?.id) {
                        this.refresh();
                    }
                });
            },

            mount(...args) {
                // mount
                this.refresh();
            },

            refresh(...args) {
                //refresh
                const page  = getPage();
                state.description = page?.description || "";
            }
        }
    }
};

