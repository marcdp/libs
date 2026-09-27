// contract
export const contract = {
    description: "Provides a panel container for a wizard.",
    events: {},
    properties: {},
    methods: {},
    slots: {
        "":{
            description: "Slot for providing the content of the wizard panel."
        }
    }
};


// implementation
export default {
    style: `
        :host {display:block;}
    `,
    template: `
        <slot></slot>
    `
};
