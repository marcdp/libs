// contract
export const contract = {
    description: "Formats and displays a date and time value.",
    events: {},
    properties: {
        datetime: {type:"string", default:"", attribute:true, state:true, description:""},
        value:    {type:"string", default:"", attribute:true, state:true, description:""},
        format:   {type:"string", default:"", attribute:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    template: `
        {{ state.formattedValue }}
    `,
    state: {
        formattedValue: ""
    },
    controller({ state, events, i18n }) {
        const updateFormattedValue = () => {
            state.formattedValue = i18n.formatDateTime(state.value || state.datetime, state.format);
        };
        return {
            load(args) {
                // load
                events.on(state, ["change:value", "change:datetime", "change:format"], updateFormattedValue);
                updateFormattedValue();
            }
        };
    }
};
