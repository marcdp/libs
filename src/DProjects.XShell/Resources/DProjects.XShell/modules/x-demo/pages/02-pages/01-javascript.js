// contract
export const contract = {
    description: "Pages javascript page",
    events: {},
    properties: {},
    methods: {}
};

// export page
export default {
    template: `
        <p>
            this is the pages javascript page
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
