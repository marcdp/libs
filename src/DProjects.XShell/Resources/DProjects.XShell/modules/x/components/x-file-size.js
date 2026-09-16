import XElement from "x-element";

// utils
function formatFileSize(bytes) {
    if (bytes === -1) return '';
    if (bytes === 0) return '0 Bytes';
    let sizes = ['bytes', 'KB', 'MB', 'GB', 'TB'];
    let i = Math.floor(Math.log(bytes) / Math.log(1024)); // Determine the unit index
    let formattedSize = (bytes / Math.pow(1024, i)).toFixed(2); // Format the number to 2 decimal places
    if (i==0) formattedSize = formattedSize.replace(".00", ""); // Remove the decimal point if it's a whole number
    return `${formattedSize} ${sizes[i]}`;
}

// export
export default {
    template: `
        {{ state.valueFormatted || ''}}
    `,
    state: {
        value: {value:0, attr:true, type:"int"},
        valueFormatted: {value:null}
    },
    script({ state, events }) {
        return {
            onCommand(command, params){
                if (command == "load") {
                    //load
                    events.on(state, "change:value", (event) => {
                        state.valueFormatted = formatFileSize(event.newValue);
                    });
                } 
            }
        }
    }
}
