// contract
export const contract = {
    description: "Formats and displays a date and time value.",
    events: {},
    properties: {
        datetime: {type:"string", default:"", attr:true, state:true, description:""},
        value:    {type:"string", default:"", attr:true, state:true, description:""},
        format:   {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    template: `
        {{ i18n.formatDateTime(state.value || state.datetime, state.format) }}
    `,
    state: {
        datetime: {value:"", attr:true},
        value: {value:"", attr:true},
        format: {value:"", attr:true}
    }
};
