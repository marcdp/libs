// contract
export const contract = {
    description: "Application",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    meta: {
        title: "Application Page"
    },
    template: `
        <x-propertygrid x-prop:value="state.config.app"></x-propertygrid>
    `,    
    state:{
        config: null
    },
    controller({ state, config }) {
        return {
            load() {
               // load
               state.config = config;
            }
        };
    }
}
