// contract
export const contract = {
    description: "Index page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    style: `
        P {display:block; border:1px red solid}
    `,
    template: `
        <p>
            this is the index page
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
