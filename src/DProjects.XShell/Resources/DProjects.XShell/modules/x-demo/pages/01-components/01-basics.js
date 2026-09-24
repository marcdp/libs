// contract
export const contract = {
    description: "Basics page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the basics page
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
