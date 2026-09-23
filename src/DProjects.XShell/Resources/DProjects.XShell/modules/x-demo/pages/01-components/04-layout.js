// contract
export const contract = {
    description: "Layout page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the layout page
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
