
// contract
export const contract = {
    description: "Displays the current page title.",
    events: {},
    properties: {},
    methods: {}
};


// implementation
export default {
    style: ``,
    template: `{{ state.label }}`,
    state: {
        label: ""
    },
    script({ events, bus, state, getPage }) {
        return {
            load(...args) {
                // load
                events.on(bus, "xshell:page:load", (event)=>{
                    if (event.detail.id == getPage()?.id) {
                        this.onCommand("refresh");
                    }
                });
            },

            mount(...args) {
                // mount
                this.onCommand("refresh");
            },

            refresh(...args) {
                //refresh
                const page  = getPage();
                state.label = page?.label || "";
            }
        }
    }
};

