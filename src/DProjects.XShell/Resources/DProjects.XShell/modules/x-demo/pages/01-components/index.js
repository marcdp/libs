// contract
export const contract = {
    description: "Components page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the components page
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
