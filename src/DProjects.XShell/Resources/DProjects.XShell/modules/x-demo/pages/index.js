// contract
export const contract = {
    description: "Index page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the index page
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
