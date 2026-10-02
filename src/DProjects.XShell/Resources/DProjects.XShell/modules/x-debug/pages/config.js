// contract
export const contract = {
    description: "Config page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    style: `
    `,
    template: `
        <x-json x-prop:value="state.config" indent="2"></x-json>
    `,    
    state:{
        config: null
    },
    controller({ state, config }) {
        return {
            load(params) {
               // load
               state.config = config;
            }
        };
    }
}
