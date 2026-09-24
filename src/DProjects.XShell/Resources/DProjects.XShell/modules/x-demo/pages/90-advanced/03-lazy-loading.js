// contract
export const contract = {
    description: "Lazy loading",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the advanced page
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
