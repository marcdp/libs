
// declaration
export const declaration = {
    description: "Displays the translated form of a text value.",
    events: {},
    properties: {
        text: {type:"string", default:"", attr:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    template: `
        {{ i18n.translate(state.text) }}
    `,
    state: {
        text: {value:"", attr:true}
    }
}
