// export page
export default {
    template: `
        <p>
            this is a test page 1
        </p>
    `,    
    state: {
    },
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
