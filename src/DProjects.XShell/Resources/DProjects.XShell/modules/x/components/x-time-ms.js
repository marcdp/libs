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
        value: {type:"number", default:0, attribute:true, state:true, description:""}
    },
    methods: {}
};


// definition
export default {
    template: `
        {{ state.valueFormatted || ''}}
    `,
    state: {
        valueFormatted: null
    },
    controller({ state, events }) {
        return {
            load() {
                //load
                events.on(state, "change:value", (event)=>{
                    state.valueFormatted = formatMilliseconds(event.newValue);
                });
            }
        }
    }
};
