// contract
export const contract = {
    description: "Loader",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the loader page
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
