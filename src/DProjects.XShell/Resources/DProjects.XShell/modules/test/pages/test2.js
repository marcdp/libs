// export page
export default {
    template: `
        <p>
            this is a test page 2 
        </p>
    `,    
    state: {
        areas: {value:[]},
        selected: {value:""}
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
