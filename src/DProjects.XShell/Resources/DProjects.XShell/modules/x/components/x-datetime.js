// export
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
