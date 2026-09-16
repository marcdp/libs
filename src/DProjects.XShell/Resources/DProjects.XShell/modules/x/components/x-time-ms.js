import XElement from "x-element";

// utils
function formatMilliseconds(ms) {
    if (ms === -1) return '';
    if (ms === 0) return '0 ms';
    return `${ms} ms`;
}

// export
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
