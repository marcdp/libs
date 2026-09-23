// contract
export const contract = {
    description: "Pages markdown page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the pages markdown page
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
