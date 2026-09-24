// contract
export const contract = {
    description: "Modules",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the modules page
        </p>
    `,    
    controller({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
