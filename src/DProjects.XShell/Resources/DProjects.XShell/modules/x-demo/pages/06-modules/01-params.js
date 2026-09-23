// contract
export const contract = {
    description: "Modules params",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the modules params page
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
