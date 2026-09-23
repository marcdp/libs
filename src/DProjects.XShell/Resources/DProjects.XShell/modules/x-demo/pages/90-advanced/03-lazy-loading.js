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
    script({ state }) {
        return {
            onCommand(command, params) {
                if (command == "load") {
                   // load
                }                
            }
        };
    }
}
