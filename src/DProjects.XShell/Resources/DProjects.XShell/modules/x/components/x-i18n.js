
// export
export default {
    template: `
        {{ i18n.translate(state.text) }}
    `,
    state: {
        text: {value:"", attr:true}
    }
}