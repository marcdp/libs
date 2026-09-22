// utils
function formatMilliseconds(ms) {
    if (ms === -1) return '';
    if (ms === 0) return '0 ms';
    return `${ms} ms`;
}

// contract
export const contract = {
    description: "Formats and displays a duration in milliseconds.",
    events: {},
    properties: {
        value: {type:"number", default:0, attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    template: `
        {{ state.valueFormatted || ''}}
    `,
    state: {
        value: {value:0, attr:true, type:"number"},
        valueFormatted: {value:null}
    },
    script({ state, events }) {
        return {
            onCommand(command) {
                if (command == "load"){
                    //load
                    events.on(state, "change:value", (event)=>{
                        state.valueFormatted = formatMilliseconds(event.newValue);
                    });
                }
            }
        }
    }
};
