// contract
export const contract = {
    description: "Navigation",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the navigation page
        </p>
    `,    
    script({ state }) {
        return {
            load(params) {
               // load
            }
        };
    }
}
