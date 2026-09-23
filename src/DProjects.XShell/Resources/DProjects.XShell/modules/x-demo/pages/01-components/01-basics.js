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
