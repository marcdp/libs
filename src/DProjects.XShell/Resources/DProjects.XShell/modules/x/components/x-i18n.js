
// contract
export const contract = {
    description: "Displays the translated form of a text value.",
    events: {},
    properties: {
        text: {type:"string", default:"", attribute:true, state:true, description:""}
    },
    methods: {}
};


// implementation
export default {
    template: `
        {{ state.localizedText }}
    `,
    state: {
        localizedText: ""
    },
    controller({ state, events, i18n }) {
        const updateLocalizedText = () => {
            state.localizedText = i18n.translate(state.text);
        };
        return {
            load(args) {
                // load
                events.on(state, "change:text", updateLocalizedText);
                updateLocalizedText();
            }
        };
    }
}
